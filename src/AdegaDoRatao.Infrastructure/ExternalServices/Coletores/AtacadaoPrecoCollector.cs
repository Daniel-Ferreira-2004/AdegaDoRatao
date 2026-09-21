using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AdegaDoRatao.Infrastructure.ExternalServices.Coletores;

/// <summary>
/// Coletor de preços do Atacadão (atacadao.com.br).
///
/// O site é VTEX IO: a API pública de catálogo fica em
/// /io/api/catalog_system/pub/products/search e devolve JSON com o EAN
/// real (items[].ean) e o preço (items[].sellers[].commertialOffer).
///
/// A busca é feita pelo próprio EAN (a VTEX indexa EANs na busca textual)
/// e o resultado é confirmado comparando o campo ean do item retornado.
/// O Cloudflare exige User-Agent de navegador — configurado no HttpClient.
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

            (VtexProduct p, VtexItem i)? produto = porEan.Count > 0 ? porEan[0] : null;

            if (produto is null && itens.Count > 0 && !string.IsNullOrWhiteSpace(nomeProduto))
            {
                produto = itens
                    .Select(x => (x, Score: NomeProdutoMatcher.Pontuar(nomeProduto, x.p.ProductName)))
                    .Where(x => x.Score >= NomeProdutoMatcher.ScoreMinimo)
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
            var oferta = encontrado.i.Sellers?.FirstOrDefault()?.Offer;
            var preco = oferta?.Price is > 0 ? oferta.Price : null;
            var disponivel = oferta is { AvailableQuantity: > 0 } && preco is not null;

            // A VTEX expõe o link relativo do produto no campo "link".
            var url = encontrado.p.Link is not null
                ? $"https://www.atacadao.com.br{encontrado.p.Link}"
                : null;

            return Result<ColetaPrecoRede>.Success(new ColetaPrecoRede(
                Rede, encontrado.p.ProductName, preco, disponivel, url));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "Atacadão: falha ao coletar o EAN {Ean}.", ean);
            return Result<ColetaPrecoRede>.Failure("Falha ao consultar a API do Atacadão.");
        }
    }

    private sealed record VtexProduct(
        [property: JsonPropertyName("productName")] string? ProductName,
        [property: JsonPropertyName("link")] string? Link,
        [property: JsonPropertyName("items")] List<VtexItem>? Items);

    private sealed record VtexItem(
        [property: JsonPropertyName("ean")] string? Ean,
        [property: JsonPropertyName("sellers")] List<VtexSeller>? Sellers);

    private sealed record VtexSeller(
        [property: JsonPropertyName("commertialOffer")] VtexOffer? Offer);

    private sealed record VtexOffer(
        [property: JsonPropertyName("Price")] decimal? Price,
        [property: JsonPropertyName("AvailableQuantity")] int AvailableQuantity);
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
        // Regionalização VTEX: CEP da adega (Ferraz de Vasconcelos) — sem
        // esses cookies a API retorna preço/estoque da loja errada.
        client.DefaultRequestHeaders.Add("Cookie", "postalCode=08503-000; region=08503000");
    }
}
