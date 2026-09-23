using System.Text.RegularExpressions;
using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Enums;
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

            // Fecha o modal de cookies/boas-vindas se aparecer.
            try
            {
                await page.ClickAsync("button:has-text('Fechar modal')", new PageClickOptions { Timeout = 3000 });
            }
            catch (TimeoutException) { /* sem modal */ }

            // Aguarda o SPA renderizar o campo de busca.
            var campo = page.Locator("#search-term, input[placeholder*='O que você precisa']").First;
            await campo.WaitForAsync(new LocatorWaitForOptions { Timeout = 15000 });

            var termo = string.IsNullOrWhiteSpace(nomeProduto) ? ean : nomeProduto;

            // Clica no campo e digita tecla por tecla — o autocomplete
            // Angular só dispara com eventos de teclado reais, não com
            // FillAsync programático.
            await campo.ClickAsync();
            await campo.PressSequentiallyAsync(termo, new LocatorPressSequentiallyOptions { Delay = 80 });

            // Aguarda o DROPDOWN do autocomplete (nav "Menu de busca").
            // Não usar 'a[href*=/produto/]' sozinho: a home já tem links
            // de produto nos carrosséis, o que faria o coletor ler os
            // produtos errados (ex.: "Tamanho de Fralda").
            try
            {
                await page.WaitForSelectorAsync("nav:has-text('Menu de busca') a[href*='/produto/'], nav[aria-label='Menu de busca'] a[href*='/produto/']",
                    new PageWaitForSelectorOptions { Timeout = 12000 });
            }
            catch (TimeoutException)
            {
                logger.LogWarning("Shibata: autocomplete não retornou produtos para '{Termo}'.", termo);
            }

            // Retorna JSON string e desserializa com System.Text.Json —
            // o conversor do Playwright quebra (NRE) quando o JS devolve
            // objetos com propriedades null.
            // Lê APENAS os links dentro do dropdown do autocomplete (nav
            // "Menu de busca"), nunca os carrosséis da home.
            var json = await page.EvaluateAsync<string>(
                @"() => {
                    const nav = document.querySelector('nav[aria-label=""Menu de busca""]')
                        ?? [...document.querySelectorAll('nav')].find(n => n.textContent.includes('Produtos'));
                    if (!nav) return '[]';
                    return JSON.stringify([...nav.querySelectorAll('a[href*=""/produto/""]')].map(a => {
                        const nome = a.querySelector('p')?.textContent?.trim() ?? '';
                        const m = a.innerText.match(/R\$\s*([\d.,]+)/);
                        return { nome, preco: m ? m[1] : null, url: a.getAttribute('href') };
                    }).filter(x => x.nome.length > 0));
                }");

            var itens = string.IsNullOrWhiteSpace(json)
                ? []
                : System.Text.Json.JsonSerializer.Deserialize<List<ProdutoShibata>>(json) ?? [];

            logger.LogInformation("Shibata: {Total} produtos no autocomplete para '{Termo}': {Itens}",
                itens.Count, termo,
                string.Join(" | ", itens.Select(x => $"{x.Nome} [{x.Preco}]")));

            if (itens.Count == 0)
            {
                return Result<ColetaPrecoRede>.Success(
                    new ColetaPrecoRede(Rede, null, null, Disponivel: false));
            }

            // Escolhe o produto com melhor score de tokens.
            var pontuados = itens
                .Select(x => (Item: x, Score: NomeProdutoMatcher.Pontuar(termo, x.Nome)))
                .ToList();

            foreach (var p in pontuados)
            {
                logger.LogInformation("Shibata: score {Score:F2} para '{Nome}'", p.Score, p.Item.Nome);
            }

            var melhor = pontuados
                .Where(x => x.Score >= NomeProdutoMatcher.ScoreMinimo
                    && NomeProdutoMatcher.ContemTokensObrigatorios(termo, x.Item.Nome))
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            if (melhor.Item is null)
            {
                logger.LogWarning(
                    "Shibata: nenhum resultado do autocomplete casou com os tokens obrigatórios de '{Termo}'.", termo);
                return Result<ColetaPrecoRede>.Success(
                    new ColetaPrecoRede(Rede, null, null, Disponivel: false));
            }

            var preco = ParsePreco(melhor.Item.Preco);
            var url = melhor.Item.Url is not null
                ? new Uri(new Uri("https://www.loja.shibata.com.br"), melhor.Item.Url).ToString()
                : null;

            // O autocomplete nem sempre mostra preço (ex.: variação sem
            // estoque na loja padrão). Nesse caso abre a página do
            // produto e tenta ler o preço no corpo da página.
            if (preco is null && url is not null)
            {
                preco = await TentarPrecoNaPaginaDoProduto(page, url, melhor.Item.Nome);
            }

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
            logger.LogWarning(ex, "Shibata: falha ao coletar o EAN {Ean} via Playwright.", ean);
            return Result<ColetaPrecoRede>.Failure("Falha ao consultar o site do Shibata.");
        }
    }

    /// <summary>
    /// Abre a página do produto e tenta extrair o preço do texto
    /// ("R$ X,XX"). Retorna null se a página não exibir preço.
    /// </summary>
    private async Task<decimal?> TentarPrecoNaPaginaDoProduto(IPage page, string url, string nome)
    {
        try
        {
            await page.GotoAsync(url,
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 20000 });
            var conteudo = await page.EvaluateAsync<string>("() => document.body.innerText");
            var m = Regex.Match(conteudo ?? string.Empty, @"R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})");
            if (m.Success)
            {
                return ParsePreco(m.Groups[1].Value);
            }

            logger.LogWarning("Shibata: página de '{Nome}' não exibe preço.", nome);
            return null;
        }
        catch (Exception ex) when (ex is TimeoutException or PlaywrightException)
        {
            logger.LogWarning(ex, "Shibata: falha ao abrir a página do produto '{Nome}'.", nome);
            return null;
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

        [System.Text.Json.Serialization.JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}
