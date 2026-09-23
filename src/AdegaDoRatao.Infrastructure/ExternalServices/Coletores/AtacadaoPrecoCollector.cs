using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AdegaDoRatao.Infrastructure.ExternalServices.Coletores;

/// <summary>
/// Coletor de preços do Atacadão (atacadao.com.br).
///
/// O site é VTEX IO (FastStore). A busca de produtos usa a API pública
/// de catálogo (/io/api/catalog_system/pub/products/search), que devolve
/// o EAN real (items[].ean) e o SKU (items[].itemId).
///
/// ATENÇÃO: o preço da API de catálogo NÃO é o preço exibido no site —
/// ele vem zerado ou de outra loja. O preço real (regionalizado) é o da
/// SIMULAÇÃO DE CHECKOUT (/api/checkout/pub/orderForms/simulation) com o
/// seller da loja da região (ex.: atacadaobr733 = Ferraz de Vasconcelos),
/// que é a mesma chamada que o front-end faz. O seller é resolvido pela
/// API de regiões (/api/checkout/pub/regions?postalCode=...).
///
/// A busca é feita pelo nome do produto (a VTEX nem sempre indexa EAN na
/// busca textual) e o resultado é confirmado comparando o campo ean do
/// item retornado. O Cloudflare exige User-Agent de navegador —
/// configurado no HttpClient.
/// </summary>
public sealed class AtacadaoPrecoCollector(
    HttpClient http,
    ILogger<AtacadaoPrecoCollector> logger) : IPrecoRedeCollector
{
    public string Rede => "Atacadão";

    // Canais de venda (sales channels) consultados — cada sc é uma loja.
    // O estoque/preço varia por loja, então consultamos todos e
    // preferimos o item DISPONÍVEL.
    private static readonly int[] SalesChannels = [1, 2];

    public async Task<Result<ColetaPrecoRede>> ColetarAsync(string ean, string? nomeProduto = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Prefere o nome do produto (a VTEX nem sempre indexa EAN na
            // busca textual); cai para o EAN quando não há nome.
            var termo = string.IsNullOrWhiteSpace(nomeProduto) ? ean : nomeProduto;

            // Consulta todos os canais e junta os itens.
            var itens = new List<(VtexProduct p, VtexItem i)>();
            foreach (var sc in SalesChannels)
            {
                var produtos = await http.GetFromJsonAsync<List<VtexProduct>>(
                    $"io/api/catalog_system/pub/products/search?ft={Uri.EscapeDataString(termo)}&_from=0&_to=5&sc={sc}",
                    cancellationToken);
                if (produtos is not null)
                {
                    itens.AddRange(produtos.SelectMany(p => p.Items ?? [], (p, i) => (p, i)));
                }
            }

            // 1) Confirma pelo EAN quando o item o expõe — preferindo o
            //    que está disponível (com preço e estoque).
            // 2) Senão, escolhe o resultado cujo nome melhor casa com o
            //    termo buscado (tokens: "doritos" + "120" etc.), também
            //    preferindo o disponível.
            static bool Disponivel((VtexProduct p, VtexItem i) x)
            {
                var o = x.i.Sellers?.FirstOrDefault()?.Offer;
                return o is { AvailableQuantity: > 0 } && o.Price is > 0;
            }

            var porEan = itens
                .Where(x => string.Equals(x.i.Ean, ean, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(Disponivel)
                .ToList();

            // Confirmação por EAN = correspondência exata do produto.
            var confirmadoPorEan = porEan.Count > 0;
            (VtexProduct p, VtexItem i)? produto = confirmadoPorEan ? porEan[0] : null;

            if (produto is null && itens.Count > 0 && !string.IsNullOrWhiteSpace(nomeProduto))
            {
                produto = itens
                    .Select(x => (x, Score: NomeProdutoMatcher.Pontuar(nomeProduto, x.p.ProductName)))
                    .Where(x => x.Score >= NomeProdutoMatcher.ScoreMinimo
                        && NomeProdutoMatcher.ContemTokensObrigatorios(nomeProduto, x.x.p.ProductName))
                    .OrderByDescending(x => Disponivel(x.x))
                    .ThenByDescending(x => x.Score)
                    .Select(x => ((VtexProduct p, VtexItem i)?)x.x)
                    .FirstOrDefault();
            }

            if (produto is null || produto.Value.i is null)
            {
                return Result<ColetaPrecoRede>.Success(
                    new ColetaPrecoRede(Rede, null, null, Disponivel: false));
            }

            var encontrado = produto.Value;

            // A VTEX expõe o link do produto no campo "link". Ele pode vir
            // relativo ("/produto/p") ou absoluto ("https://secure.atacadao.com.br/..."),
            // então só prefixamos o domínio quando for relativo.
            var url = encontrado.p.Link switch
            {
                null => null,
                var l when l.StartsWith("http", StringComparison.OrdinalIgnoreCase) => l,
                var l => $"https://www.atacadao.com.br{l}"
            };

            // O preço REAL (o mesmo exibido no site) vem da simulação de
            // checkout com o seller da loja da região — a API de catálogo
            // retorna preço zerado ou de outra loja.
            decimal? preco = null;
            var disponivel = false;
            var tipoPreco = TipoPrecoColeta.Normal;
            if (!string.IsNullOrWhiteSpace(encontrado.i.ItemId))
            {
                (preco, disponivel, tipoPreco) = await TentarPrecoNaSimulacao(
                    encontrado.i.ItemId, encontrado.p.ProductName, cancellationToken);
            }

            // Fallback: baixa o HTML da página e extrai o preço do JSON-LD.
            if (preco is null && url is not null)
            {
                preco = await TentarPrecoNaPagina(url, encontrado.p.ProductName, cancellationToken);
                disponivel = preco is not null;
            }

            // Região CONFIRMADA: o preço vem da simulação de checkout com
            // o CEP da adega + seller regional (ou da página com os
            // cookies de CEP). Confiança: High com EAN, Medium só por nome.
            var confianca = preco is null
                ? NivelConfiancaColeta.Unverified
                : confirmadoPorEan ? NivelConfiancaColeta.High : NivelConfiancaColeta.Medium;

            return Result<ColetaPrecoRede>.Success(new ColetaPrecoRede(
                Rede, encontrado.p.ProductName, preco, disponivel, url,
                tipoPreco, confianca, RegiaoConfirmada: true));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "Atacadão: falha ao coletar o EAN {Ean}.", ean);
            return Result<ColetaPrecoRede>.Failure("Falha ao consultar a API do Atacadão.");
        }
    }

    // CEP da adega (08533-020, nº 126 — Ferraz de Vasconcelos) — usado
    // para regionalizar o preço. No site equivale a: botão "CEP" →
    // "Entrega em casa" → CEP → número 126 → confirmar.
    private const string CepAdega = "08533-020";

    // Seller padrão da loja Ferraz de Vasconcelos (usado se a API de
    // regiões falhar). Descoberto via /api/checkout/pub/regions.
    private const string SellerFallback = "atacadaobr733";

    // Cache do seller regional (não muda entre coletas).
    private static string? _sellerRegional;
    private static readonly SemaphoreSlim _sellerLock = new(1, 1);

    /// <summary>
    /// Obtém o preço real do SKU via simulação de checkout da VTEX — a
    /// mesma chamada que o front-end do site faz.
    ///
    /// Estratégia de preço (validada na API real em 2026-09-23):
    /// 1) simula 1 UNIDADE → preço unitário normal (fallback: 6 unid.);
    /// 2) simula 12 UNIDADES → o site destaca descontos "a partir de N
    ///    unid." (ex.: Cerveja Original 350ml: R$ 5,19 → R$ 4,99 em 12);
    ///    quando o preço em 12 é menor, devolvemos ESSE preço, marcado
    ///    como <see cref="TipoPrecoColeta.CondicionadoQuantidade"/> — é o
    ///    preço em destaque no site, sem nunca misturá-lo com o normal.
    /// Retorna (preço em reais, disponível, tipo do preço).
    /// </summary>
    private async Task<(decimal? Preco, bool Disponivel, TipoPrecoColeta Tipo)> TentarPrecoNaSimulacao(
        string skuId, string? nome, CancellationToken cancellationToken)
    {
        var seller = await ObterSellerRegional(cancellationToken);

        // 1) Preço unitário (1 unid.; fallback 6 quando sem preço em 1).
        var simUnitaria = await SimularAsync(skuId, 1, seller, nome, cancellationToken)
            ?? await SimularAsync(skuId, 6, seller, nome, cancellationToken);
        if (simUnitaria is null)
        {
            return (null, false, TipoPrecoColeta.Normal);
        }

        var (unitario, disponivel) = simUnitaria.Value;

        // 2) Desconto por quantidade: simula 12 unidades; se o preço cair,
        //    devolve o preço condicionado (o destaque do site).
        var simAtacado = await SimularAsync(skuId, 12, seller, nome, cancellationToken);
        if (simAtacado?.Preco is > 0 && simAtacado.Value.Preco < unitario)
        {
            return (simAtacado.Value.Preco, disponivel, TipoPrecoColeta.CondicionadoQuantidade);
        }

        return (unitario, disponivel, TipoPrecoColeta.Normal);
    }

    /// <summary>
    /// Simula o checkout para uma quantidade e devolve o preço unitário
    /// efetivo (em reais) e a disponibilidade; null quando sem preço.
    /// A VTEX devolve o preço em CENTAVOS (ex.: 1229 = R$ 12,29). Quando
    /// há tiers (priceDefinition.sellingPrices), o sellingPrice é o do
    /// tier da quantidade simulada; para qtd=1 garantimos o tier unitário
    /// quando exposto.
    /// </summary>
    private async Task<(decimal Preco, bool Disponivel)?> SimularAsync(
        string skuId, int quantidade, string seller, string? nome, CancellationToken cancellationToken)
    {
        try
        {
            var payload = new
            {
                items = new[] { new { id = skuId, quantity = quantidade, seller } },
                postalCode = CepAdega,
                country = "BRA"
            };

            var resposta = await http.PostAsJsonAsync(
                "api/checkout/pub/orderForms/simulation?sc=1", payload, cancellationToken);
            if (!resposta.IsSuccessStatusCode)
            {
                return null;
            }

            var simulacao = await resposta.Content
                .ReadFromJsonAsync<VtexSimulacao>(cancellationToken: cancellationToken);
            var item = simulacao?.Items?.FirstOrDefault();
            if (item?.SellingPrice is not > 0)
            {
                return null;
            }

            var valor = quantidade == 1
                ? item.PriceDefinition?.SellingPrices?
                    .Where(t => t.Quantity <= 1 && t.Value > 0)
                    .OrderBy(t => t.Quantity)
                    .FirstOrDefault()?.Value ?? item.SellingPrice.Value
                : item.SellingPrice.Value;

            return (valor / 100m,
                string.Equals(item.Availability, "available", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "Atacadão: falha na simulação de checkout da SKU {Sku} ('{Nome}', qtd={Qtd}).", skuId, nome, quantidade);
            return null;
        }
    }

    /// <summary>
    /// Resolve o seller da loja que atende o CEP da adega via API de
    /// regiões da VTEX (prefere a loja de Ferraz de Vasconcelos). O
    /// resultado é cacheado — a loja não muda entre coletas.
    /// </summary>
    private async Task<string> ObterSellerRegional(CancellationToken cancellationToken)
    {
        if (_sellerRegional is not null)
        {
            return _sellerRegional;
        }

        await _sellerLock.WaitAsync(cancellationToken);
        try
        {
            if (_sellerRegional is not null)
            {
                return _sellerRegional;
            }

            var regioes = await http.GetFromJsonAsync<List<VtexRegiao>>(
                $"api/checkout/pub/regions?postalCode={CepAdega}&country=BRA", cancellationToken);
            var sellers = regioes?.FirstOrDefault()?.Sellers;
            _sellerRegional = sellers?.FirstOrDefault(s => s.Name?.Contains("Ferraz", StringComparison.OrdinalIgnoreCase) == true)?.Id
                ?? sellers?.FirstOrDefault()?.Id
                ?? SellerFallback;
            logger.LogInformation("Atacadão: seller regional resolvido = {Seller}.", _sellerRegional);
            return _sellerRegional;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "Atacadão: falha ao resolver o seller regional; usando {Seller}.", SellerFallback);
            return SellerFallback;
        }
        finally
        {
            _sellerLock.Release();
        }
    }

    /// <summary>
    /// Baixa a página do produto e extrai o preço do bloco JSON-LD
    /// (schema.org Product → offers.price), que é o preço real exibido
    /// no site. A API de catálogo nem sempre traz o preço (vem 0 quando
    /// o item está sem estoque no canal), mas a página é renderizada no
    /// servidor com o preço regionalizado.
    ///
    /// NÃO usar o primeiro "R$ X,XX" do HTML: ele pode ser o "Resumo da
    /// compra" de outra SKU ou de uma seção de relacionados, gerando
    /// preço errado. Fallback: primeiro "R$ X,XX" só se o JSON-LD falhar.
    /// Retorna null se não encontrar.
    /// </summary>
    private async Task<decimal?> TentarPrecoNaPagina(string url, string? nome, CancellationToken cancellationToken)
    {
        try
        {
            var html = await http.GetStringAsync(url, cancellationToken);

            // 1) JSON-LD: {"@type":"Product",...,"offers":{"price":9.99,...}}
            var jsonLd = System.Text.RegularExpressions.Regex.Match(
                html, @"""@type""\s*:\s*""Product"".*?""offers""\s*:\s*\{\s*""price""\s*:\s*([\d]+(?:\.[\d]+)?)",
                System.Text.RegularExpressions.RegexOptions.Singleline);
            if (jsonLd.Success
                && decimal.TryParse(jsonLd.Groups[1].Value,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var precoJsonLd)
                && precoJsonLd > 0)
            {
                return precoJsonLd;
            }

            // 2) Fallback: primeiro "R$ X,XX" do HTML.
            var m = System.Text.RegularExpressions.Regex.Match(
                html, @"R\$\s*(?:&nbsp;)?\s*(\d{1,3}(?:\.\d{3})*,\d{2})");
            if (!m.Success)
            {
                logger.LogWarning("Atacadão: página de '{Nome}' não exibe preço.", nome);
                return null;
            }

            var texto = m.Groups[1].Value.Replace(".", "").Replace(",", ".");
            return decimal.TryParse(texto, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var valor) ? valor : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Atacadão: falha ao ler a página do produto '{Nome}'.", nome);
            return null;
        }
    }

    private sealed record VtexProduct(
        [property: JsonPropertyName("productName")] string? ProductName,
        [property: JsonPropertyName("link")] string? Link,
        [property: JsonPropertyName("items")] List<VtexItem>? Items);

    private sealed record VtexItem(
        [property: JsonPropertyName("itemId")] string? ItemId,
        [property: JsonPropertyName("ean")] string? Ean,
        [property: JsonPropertyName("sellers")] List<VtexSeller>? Sellers);

    private sealed record VtexSeller(
        [property: JsonPropertyName("commertialOffer")] VtexOffer? Offer);

    private sealed record VtexOffer(
        [property: JsonPropertyName("Price")] decimal? Price,
        [property: JsonPropertyName("AvailableQuantity")] int AvailableQuantity);

    private sealed record VtexSimulacao(
        [property: JsonPropertyName("items")] List<VtexSimulacaoItem>? Items);

    private sealed record VtexSimulacaoItem(
        [property: JsonPropertyName("sellingPrice")] int? SellingPrice,
        [property: JsonPropertyName("availability")] string? Availability,
        [property: JsonPropertyName("priceDefinition")] VtexPriceDefinition? PriceDefinition);

    private sealed record VtexPriceDefinition(
        [property: JsonPropertyName("sellingPrices")] List<VtexSellingPriceTier>? SellingPrices);

    private sealed record VtexSellingPriceTier(
        [property: JsonPropertyName("quantity")] int Quantity,
        [property: JsonPropertyName("value")] int Value);

    private sealed record VtexRegiao(
        [property: JsonPropertyName("sellers")] List<VtexRegiaoSeller>? Sellers);

    private sealed record VtexRegiaoSeller(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("name")] string? Name);
}

/// <summary>Configuração do HttpClient do Atacadão (registrada na DI).</summary>
public static class AtacadaoPrecoCollectorSetup
{
    public static void Configurar(HttpClient client)
    {
        client.BaseAddress = new Uri("https://www.atacadao.com.br/");
        client.Timeout = TimeSpan.FromSeconds(20);
        // O Cloudflare do Atacadão bloqueia requisições sem User-Agent de navegador.
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        // Regionalização VTEX: CEP da adega (08533-020, nº 126, Ferraz de
        // Vasconcelos) — sem esses cookies a API retorna preço/estoque da
        // loja errada.
        client.DefaultRequestHeaders.Add("Cookie", "postalCode=08533-020; region=08533020");
    }
}
