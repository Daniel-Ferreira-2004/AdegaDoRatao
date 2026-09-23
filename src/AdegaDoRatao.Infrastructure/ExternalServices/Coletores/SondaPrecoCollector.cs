using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AdegaDoRatao.Infrastructure.ExternalServices.Coletores;

/// <summary>
/// Coletor de preços do Sonda (sondadelivery.com.br) via Playwright.
///
/// O site é ASP.NET WebForms, mas a busca funciona direto pela URL:
///   /delivery/busca/{termo}
/// Os resultados renderizam como cartões ".product" contendo:
///   - nome:  span.tit
///   - preço: texto "Por R$ 14,73" dentro do cartão
///
/// O produto é escolhido pelo melhor score de tokens
/// (<see cref="NomeProdutoMatcher"/>) entre os resultados.
///
/// ATENÇÃO: lento (5-15s por consulta) e sensível a mudanças de layout.
/// Deve rodar apenas no job diário, nunca em tempo real.
/// </summary>
public sealed class SondaPrecoCollector(ILogger<SondaPrecoCollector> logger) : IPrecoRedeCollector
{
    public string Rede => "Sonda";

    public async Task<Result<ColetaPrecoRede>> ColetarAsync(string ean, string? nomeProduto = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            var page = await browser.NewPageAsync();

            var termo = string.IsNullOrWhiteSpace(nomeProduto) ? ean : nomeProduto;
            await page.GotoAsync($"https://www.sondadelivery.com.br/delivery/busca/{Uri.EscapeDataString(termo)}",
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30000 });

            // Aguarda os cartões de produto renderizarem.
            try
            {
                await page.WaitForSelectorAsync(".product",
                    new PageWaitForSelectorOptions { Timeout = 12000 });
            }
            catch (TimeoutException)
            {
                logger.LogWarning("Sonda: nenhum produto encontrado para '{Termo}'.", termo);
            }

            // JSON string + System.Text.Json: o conversor do Playwright
            // quebra (NRE) com propriedades null.
            var json = await page.EvaluateAsync<string>(
                @"() => JSON.stringify([...document.querySelectorAll('.product')].map(c => {
                    const nome = c.querySelector('.tit')?.textContent?.trim() ?? '';
                    const m = c.innerText.match(/Por\s*R\$\s*([\d.,]+)/) ?? c.innerText.match(/R\$\s*([\d.,]+)/);
                    const link = c.querySelector('a[href]')?.getAttribute('href') ?? null;
                    return { nome, preco: m ? m[1] : null, url: link };
                }).filter(x => x.nome.length > 0))");

            var itens = string.IsNullOrWhiteSpace(json)
                ? []
                : System.Text.Json.JsonSerializer.Deserialize<List<ProdutoSonda>>(json) ?? [];

            logger.LogInformation("Sonda: {Total} produtos para '{Termo}': {Itens}",
                itens.Count, termo,
                string.Join(" | ", itens.Take(5).Select(x => $"{x.Nome} [{x.Preco}]")));

            if (itens.Count == 0)
            {
                return Result<ColetaPrecoRede>.Success(
                    new ColetaPrecoRede(Rede, null, null, Disponivel: false));
            }

            // Escolhe o produto com melhor score de tokens, exigindo os
            // tokens obrigatórios (medidas como "2l" e marca como "coca").
            var melhor = itens
                .Select(x => (Item: x, Score: NomeProdutoMatcher.Pontuar(termo, x.Nome)))
                .Where(x => x.Score >= NomeProdutoMatcher.ScoreMinimo
                    && NomeProdutoMatcher.ContemTokensObrigatorios(termo, x.Item.Nome))
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            if (melhor.Item is null)
            {
                return Result<ColetaPrecoRede>.Success(
                    new ColetaPrecoRede(Rede, null, null, Disponivel: false));
            }

            var preco = ShibataPrecoCollector.ParsePreco(melhor.Item.Preco);
            var url = melhor.Item.Url is not null
                ? new Uri(new Uri("https://www.sondadelivery.com.br"), melhor.Item.Url).ToString()
                : null;

            // REGIÃO NÃO CONFIRMADA e SEM EAN: o site não expõe o EAN e o
            // preço é da loja padrão — confiança baixa por definição.
            return Result<ColetaPrecoRede>.Success(new ColetaPrecoRede(
                Rede, melhor.Item.Nome, preco, preco is not null, url,
                TipoPrecoColeta.Normal,
                preco is null ? NivelConfiancaColeta.Unverified : NivelConfiancaColeta.Low,
                RegiaoConfirmada: false));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Sonda: falha ao coletar o EAN {Ean} via Playwright.", ean);
            return Result<ColetaPrecoRede>.Failure("Falha ao consultar o site do Sonda.");
        }
    }

    private sealed class ProdutoSonda
    {
        [System.Text.Json.Serialization.JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("preco")]
        public string? Preco { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}
