using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AdegaDoRatao.Infrastructure.ExternalServices.Coletores;

/// <summary>
/// Coletor de preços do Shibata (loja.shibata.com.br) via Playwright.
///
/// O site é um SPA Angular da plataforma "Supermercados Online" cuja API
/// exige tokens de sessão gerados no navegador — por isso a coleta usa um
/// navegador real (Chromium headless): abre a página de busca pelo EAN,
/// aguarda a renderização e lê o preço exibido.
///
/// ATENÇÃO: este coletor é lento (3-10s por consulta) e sensível a mudanças
/// de layout. Deve rodar apenas no job diário, nunca em tempo real.
/// </summary>
public sealed class ShibataPrecoCollector(ILogger<ShibataPrecoCollector> logger) : IPrecoRedeCollector
{
    public string Rede => "Shibata";

    public async Task<Result<ColetaPrecoRede>> ColetarAsync(string ean, CancellationToken cancellationToken = default)
    {
        try
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            var page = await browser.NewPageAsync();

            await page.GotoAsync($"https://www.loja.shibata.com.br/busca?q={Uri.EscapeDataString(ean)}",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });

            // Aguarda os cartões de produto renderizarem (ou a mensagem de vazio).
            await page.WaitForTimeoutAsync(3000);

            var conteudo = await page.ContentAsync();
            if (!conteudo.Contains(ean) && !conteudo.Contains("R$"))
            {
                return Result<ColetaPrecoRede>.Success(
                    new ColetaPrecoRede(Rede, null, null, Disponivel: false));
            }

            // Extrai o primeiro preço visível da página de resultados.
            var precoTexto = await page.EvaluateAsync<string?>(
                "() => document.querySelector('[class*=\"preco\"], [class*=\"price\"]')?.textContent ?? null");

            var preco = ParsePreco(precoTexto);
            return Result<ColetaPrecoRede>.Success(new ColetaPrecoRede(
                Rede, null, preco, preco is not null));
        }
        catch (Exception ex) when (ex is PlaywrightException or TimeoutException)
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
}
