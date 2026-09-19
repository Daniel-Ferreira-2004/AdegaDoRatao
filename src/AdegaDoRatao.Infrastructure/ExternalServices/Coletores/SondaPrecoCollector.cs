using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AdegaDoRatao.Infrastructure.ExternalServices.Coletores;

/// <summary>
/// Coletor de preços do Sonda (sondadelivery.com.br) via Playwright.
///
/// O site é ASP.NET WebForms: a busca exige ViewState de sessão e o
/// autocomplete usa PageMethods — ambos só funcionam com um navegador
/// real. O coletor abre a página, digita o EAN na busca e lê os
/// resultados renderizados.
///
/// ATENÇÃO: lento (3-10s por consulta) e sensível a mudanças de layout.
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

            await page.GotoAsync("https://www.sondadelivery.com.br",
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30000 });

            // Preenche o campo de busca e submete. Prefere o nome do
            // produto — a busca do Sonda nem sempre indexa EAN.
            var termo = string.IsNullOrWhiteSpace(nomeProduto) ? ean : nomeProduto;
            await page.FillAsync(".txt-busca-nova", termo);
            await page.Keyboard.PressAsync("Enter");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                new PageWaitForLoadStateOptions { Timeout = 30000 });

            var conteudo = await page.ContentAsync();
            if (!conteudo.Contains("R$"))
            {
                return Result<ColetaPrecoRede>.Success(
                    new ColetaPrecoRede(Rede, null, null, Disponivel: false));
            }

            var precoTexto = await page.EvaluateAsync<string?>(
                "() => document.querySelector('[class*=\"preco\"], [class*=\"price\"], .valor')?.textContent ?? null");

            var preco = ShibataPrecoCollector.ParsePreco(precoTexto);
            return Result<ColetaPrecoRede>.Success(new ColetaPrecoRede(
                Rede, null, preco, preco is not null));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Sonda: falha ao coletar o EAN {Ean} via Playwright.", ean);
            return Result<ColetaPrecoRede>.Failure("Falha ao consultar o site do Sonda.");
        }
    }
}
