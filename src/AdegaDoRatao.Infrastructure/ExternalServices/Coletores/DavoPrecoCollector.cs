using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AdegaDoRatao.Infrastructure.ExternalServices.Coletores;

/// <summary>
/// Coletor de preços do D'avó (davo.com.br) via Playwright.
///
/// O site é um SPA Angular da plataforma VipCommerce. A busca funciona
/// pela URL /busca?termo={termo}, que dispara a API interna:
///   GET services.vipcommerce.com.br/api-admin/v1/org/399/filial/1/
///       centro_distribuicao/{cd}/loja/buscas/produtos/termo/{termo}
/// A API exige um token Bearer de sessão anônima que o SPA obtém
/// automaticamente — por isso o coletor NÃO chama a API direto, e sim
/// intercepta a resposta JSON da busca renderizada pelo navegador.
///
/// REGIÃO: a loja de SUZANO é o centro_distribuicao/4. O coletor abre a
/// home, clica em "Retirar na loja" e seleciona "D'avó Suzano" — o SPA
/// grava cdSelecionado=4 e passa a consultar o CD de Suzano.
///
/// A API expõe o EAN (codigo_barras), então a confiança é High quando o
/// EAN confere. O produto é escolhido pelo melhor score de tokens
/// (<see cref="NomeProdutoMatcher"/>) entre os resultados.
///
/// ATENÇÃO: lento (5-15s por consulta) e sensível a mudanças de layout.
/// Deve rodar apenas no job diário, nunca em tempo real.
/// </summary>
public sealed class DavoPrecoCollector(ILogger<DavoPrecoCollector> logger) : IPrecoRedeCollector
{
    public string Rede => "D'avó";

    // Centro de distribuição da loja de Suzano (D'avó Suzano — Avenida
    // Armando Salles de Oliveira, 1200, Parque Suzano). Confirmado por
    // engenharia reversa: ao selecionar a loja o SPA chama
    // alterar_centro_distribuicao/omnichannel {"novo_cd_id":4}.
    private const int CdSuzano = 4;

    public async Task<Result<ColetaPrecoRede>> ColetarAsync(string ean, string? nomeProduto = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            var page = await browser.NewPageAsync();

            // Seleciona a loja de Suzano antes de buscar — o preço é por CD.
            await SelecionarLojaSuzano(page);

            var termo = string.IsNullOrWhiteSpace(nomeProduto) ? ean : nomeProduto;

            // Intercepta a resposta JSON da busca (a API exige o token de
            // sessão que só o SPA tem — não dá para chamar direto).
            string? json = null;
            void Handler(object? sender, IResponse response)
            {
                if (response.Url.Contains("buscas/produtos/termo/", StringComparison.OrdinalIgnoreCase))
                {
                    try { json = response.JsonAsync().GetAwaiter().GetResult()?.GetRawText(); }
                    catch { /* resposta não-JSON */ }
                }
            }
            page.Response += Handler;
            try
            {
                await page.GotoAsync($"https://www.davo.com.br/busca?termo={Uri.EscapeDataString(termo)}",
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 45000 });
            }
            finally
            {
                page.Response -= Handler;
            }

            var itens = Desserializar(json);

            logger.LogInformation("D'avó: {Total} produtos para '{Termo}': {Itens}",
                itens.Count, termo,
                string.Join(" | ", itens.Take(5).Select(x => $"{x.Descricao} [{x.Preco}] EAN {x.CodigoBarras}")));

            if (itens.Count == 0)
            {
                return Result<ColetaPrecoRede>.Success(
                    new ColetaPrecoRede(Rede, null, null, Disponivel: false));
            }

            // Escolhe o produto com melhor score de tokens, exigindo os
            // tokens obrigatórios (medidas como "2l" e marca como "coca").
            var melhor = itens
                .Select(x => (Item: x, Score: NomeProdutoMatcher.Pontuar(termo, x.Descricao)))
                .Where(x => x.Score >= NomeProdutoMatcher.ScoreMinimo
                    && NomeProdutoMatcher.ContemTokensObrigatorios(termo, x.Item.Descricao))
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            if (melhor.Item is null)
            {
                logger.LogWarning("D'avó: nenhum resultado casou com os tokens obrigatórios de '{Termo}'.", termo);
                return Result<ColetaPrecoRede>.Success(
                    new ColetaPrecoRede(Rede, null, null, Disponivel: false));
            }

            // Preço de oferta (quando em_oferta) tem prioridade — é o
            // preço em destaque no site.
            var preco = melhor.Item.PrecoOferta ?? melhor.Item.Preco;
            var disponivel = melhor.Item.Disponivel && preco is not null;
            var url = melhor.Item.Link is not null
                ? $"https://www.davo.com.br/produto/{melhor.Item.Link}"
                : null;

            // A API expõe o EAN: confiança High quando confere, Medium só
            // por nome. Região CONFIRMADA (CD de Suzano selecionado).
            var confirmadoPorEan = !string.IsNullOrWhiteSpace(ean)
                && string.Equals(melhor.Item.CodigoBarras?.Trim(), ean.Trim(), StringComparison.OrdinalIgnoreCase);
            var confianca = preco is null
                ? NivelConfiancaColeta.Unverified
                : confirmadoPorEan ? NivelConfiancaColeta.High : NivelConfiancaColeta.Medium;

            return Result<ColetaPrecoRede>.Success(new ColetaPrecoRede(
                Rede, melhor.Item.Descricao, preco, disponivel, url,
                TipoPrecoColeta.Normal, confianca, RegiaoConfirmada: true));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "D'avó: falha ao coletar o EAN {Ean} via Playwright.", ean);
            return Result<ColetaPrecoRede>.Failure("Falha ao consultar o site do D'avó.");
        }
    }

    /// <summary>
    /// Abre a home e seleciona a loja "D'avó Suzano" no seletor de loja,
    /// para que as buscas usem o centro de distribuição de Suzano.
    /// </summary>
    private async Task SelecionarLojaSuzano(IPage page)
    {
        try
        {
            await page.GotoAsync("https://www.davo.com.br/",
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 45000 });

            // Abre o seletor de loja ("Retirar na loja: ...").
            await page.GetByText("Retirar na loja").First.ClickAsync(
                new LocatorClickOptions { Timeout = 15000 });
            await page.WaitForTimeoutAsync(1500);

            // Seleciona "D'avó Suzano".
            await page.GetByText("D'avó Suzano").First.ClickAsync(
                new LocatorClickOptions { Timeout = 10000 });
            await page.WaitForTimeoutAsync(2500);
        }
        catch (Exception ex) when (ex is TimeoutException or PlaywrightException)
        {
            // Se não conseguir trocar de loja, segue com a loja padrão —
            // o preço pode ser de outra região (sinalizado pelo chamador
            // via RegiaoConfirmada quando necessário).
            logger.LogWarning(ex, "D'avó: não foi possível selecionar a loja de Suzano.");
        }
    }

    private static List<ProdutoDavo> Desserializar(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out var data)
                || !data.TryGetProperty("produtos", out var produtos))
            {
                return [];
            }

            var lista = new List<ProdutoDavo>();
            foreach (var p in produtos.EnumerateArray())
            {
                lista.Add(new ProdutoDavo
                {
                    Descricao = p.TryGetProperty("descricao", out var d) ? d.GetString() ?? "" : "",
                    CodigoBarras = p.TryGetProperty("codigo_barras", out var e) ? e.GetString() : null,
                    Link = p.TryGetProperty("link", out var l) ? l.GetString() : null,
                    Disponivel = p.TryGetProperty("disponivel", out var disp) && disp.GetBoolean(),
                    Preco = LerPreco(p, "preco"),
                    PrecoOferta = p.TryGetProperty("oferta", out var of) && of.ValueKind == System.Text.Json.JsonValueKind.Object
                        ? LerPreco(of, "preco_oferta")
                        : null
                });
            }
            return lista;
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }

    private static decimal? LerPreco(System.Text.Json.JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v))
        {
            return null;
        }

        // A API retorna o preço como string ("4.19") ou número.
        var texto = v.ValueKind == System.Text.Json.JsonValueKind.String
            ? v.GetString()
            : v.GetRawText();
        return decimal.TryParse(texto, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var valor) ? valor : null;
    }

    private sealed class ProdutoDavo
    {
        public string Descricao { get; set; } = string.Empty;
        public string? CodigoBarras { get; set; }
        public string? Link { get; set; }
        public bool Disponivel { get; set; }
        public decimal? Preco { get; set; }
        public decimal? PrecoOferta { get; set; }
    }
}
