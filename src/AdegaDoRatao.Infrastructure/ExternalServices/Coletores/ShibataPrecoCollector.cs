using System.Text.RegularExpressions;
using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AdegaDoRatao.Infrastructure.ExternalServices.Coletores;

/// <summary>
/// Coletor de preços do Shibata (loja.shibata.com.br) via Playwright.
///
/// O site é um SPA Angular da plataforma "Supermercados Online". A busca
/// NÃO funciona pela URL /busca?q=... (retorna "nenhum produto") — a
/// busca real é feita pelo AUTOCOMPLETE da home: digita-se o termo no
/// campo "O que você precisa?" e os resultados aparecem num dropdown com
/// links /produto/{id}/{slug}, cada um contendo nome (tag p) e preço
/// (texto "R$ X,XX").
///
/// O produto é escolhido pelo melhor score de tokens
/// (<see cref="NomeProdutoMatcher"/>) entre os resultados do autocomplete.
///
/// ATENÇÃO: lento (5-15s por consulta) e sensível a mudanças de layout.
/// Deve rodar apenas no job diário, nunca em tempo real.
/// </summary>
public sealed partial class ShibataPrecoCollector(ILogger<ShibataPrecoCollector> logger) : IPrecoRedeCollector
{
    public string Rede => "Shibata";

    public async Task<Result<ColetaPrecoRede>> ColetarAsync(string ean, string? nomeProduto = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            var page = await browser.NewPageAsync();

            await page.GotoAsync("https://www.loja.shibata.com.br/",
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30000 });

            // Aguarda o SPA renderizar o campo de busca.
            var campo = page.Locator("input[placeholder*='O que você precisa'], input[type='search'], input[placeholder*='busca' i]").First;
            await campo.WaitForAsync(new LocatorWaitForOptions { Timeout = 15000 });

            var termo = string.IsNullOrWhiteSpace(nomeProduto) ? ean : nomeProduto;
            await campo.FillAsync(termo);

            // Aguarda o dropdown de autocomplete renderizar os produtos.
            // Se não aparecer nada em 10s, segue com lista vazia (o
            // EvaluateAsync abaixo devolve null nesse caso).
            try
            {
                await page.WaitForSelectorAsync("a[href*='/produto/']",
                    new PageWaitForSelectorOptions { Timeout = 10000 });
            }
            catch (TimeoutException)
            {
                // sem resultados no autocomplete — tratado abaixo
            }

            var itens = await page.EvaluateAsync<List<ProdutoShibata>>(
                @"() => [...document.querySelectorAll('a[href*=""/produto/""]')].map(a => {
                    const nome = a.querySelector('p')?.textContent?.trim() ?? '';
                    const m = a.innerText.match(/R\$\s*([\d.,]+)/);
                    return { nome, preco: m ? m[1] : null };
                }).filter(x => x.nome.length > 0)") ?? [];

            if (itens.Count == 0)
            {
                return Result<ColetaPrecoRede>.Success(
                    new ColetaPrecoRede(Rede, null, null, Disponivel: false));
            }

            // Escolhe o produto com melhor score de tokens.
            var melhor = itens
                .Select(x => (Item: x, Score: NomeProdutoMatcher.Pontuar(termo, x.Nome)))
                .Where(x => x.Score >= NomeProdutoMatcher.ScoreMinimo)
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            if (melhor.Item is null)
            {
                return Result<ColetaPrecoRede>.Success(
                    new ColetaPrecoRede(Rede, null, null, Disponivel: false));
            }

            var preco = ParsePreco(melhor.Item.Preco);
            return Result<ColetaPrecoRede>.Success(new ColetaPrecoRede(
                Rede, melhor.Item.Nome, preco, preco is not null));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Shibata: falha ao coletar o EAN {Ean} via Playwright.", ean);
            return Result<ColetaPrecoRede>.Failure("Falha ao consultar o site do Shibata.");
        }
    }

    internal static decimal? ParsePreco(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        var limpo = texto.Replace("R$", "").Trim()
            .Replace(".", "").Replace(",", ".");
        return decimal.TryParse(limpo, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var valor) ? valor : null;
    }

    private sealed class ProdutoShibata
    {
        [System.Text.Json.Serialization.JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("preco")]
        public string? Preco { get; set; }
    }
}
