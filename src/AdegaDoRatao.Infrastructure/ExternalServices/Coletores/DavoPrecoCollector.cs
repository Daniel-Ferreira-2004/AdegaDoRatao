using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AdegaDoRatao.Infrastructure.ExternalServices.Coletores;

/// <summary>
/// Coletor de preços do D'avó (davo.com.br) via Playwright.
///
/// O site é um SPA Angular da plataforma VipCommerce. A busca é feita
/// pela API interna:
///   GET services.vipcommerce.com.br/api-admin/v1/org/399/filial/1/
///       centro_distribuicao/{cd}/loja/buscas/produtos/termo/{termo}
/// A API exige um token Bearer de sessão anônima que o SPA obtém
/// automaticamente ao carregar. O coletor abre a home no Playwright,
/// CAPTURA o header Authorization (e o sessao-id) das requisições do SPA
/// e então chama a API de busca diretamente com esses headers — sem
/// depender de cliques na UI (frágeis em headless).
///
/// REGIÃO: a loja de SUZANO é o centro_distribuicao/4. O CD vai no PATH
/// da busca, então basta consultar /centro_distribuicao/4/ — não é
/// preciso selecionar a loja na UI. Confirmado por engenharia reversa:
/// ao selecionar "D'avó Suzano" o SPA passa a usar cd=4 nas buscas.
///
/// A API expõe o EAN (codigo_barras), então a confiança é High quando o
/// EAN confere. O produto é escolhido pelo melhor score de tokens
/// (<see cref="NomeProdutoMatcher"/>) entre os resultados.
///
/// ATENÇÃO: lento (5-15s por consulta). Deve rodar apenas no job diário,
/// nunca em tempo real.
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
            // Contexto com UA/viewport de navegador real — o SPA pode não
            // renderizar o seletor de loja em headless "cru".
            await using var context = await browser.NewContextAsync(new()
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1366, Height = 900 },
                Locale = "pt-BR"
            });
            var page = await context.NewPageAsync();

            // O SPA envia "Authorization: Bearer" (vazio) na 1ª carga e,
            // após receber o token (X-Auth-Upgrade), passa a enviá-lo
            // completo nas requisições seguintes. Captura o PRIMEIRO
            // authorization com token real (mais que "Bearer").
            string? authorization = null;
            string? sessaoId = null;
            void RequestHandler(object? sender, IRequest request)
            {
                if (!request.Url.Contains("services.vipcommerce.com.br", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (sessaoId is null
                    && request.Headers.TryGetValue("sessao-id", out var sid)
                    && !string.IsNullOrWhiteSpace(sid))
                {
                    sessaoId = sid;
                }

                if (authorization is null
                    && request.Headers.TryGetValue("authorization", out var auth)
                    && !string.IsNullOrWhiteSpace(auth)
                    && auth.Length > 10) // ignora "Bearer" vazio
                {
                    authorization = auth;
                }
            }
            page.Request += RequestHandler;
            try
            {
                await page.GotoAsync("https://www.davo.com.br/",
                    new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 45000 });

                // Navega para uma busca para forçar o SPA a usar o token
                // já atualizado nas chamadas autenticadas.
                await page.GotoAsync("https://www.davo.com.br/busca?termo=a",
                    new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 45000 });

                var aguardado = 0;
                while (authorization is null && aguardado < 25000)
                {
                    await page.WaitForTimeoutAsync(500);
                    aguardado += 500;
                }
            }
            finally
            {
                page.Request -= RequestHandler;
            }

            if (authorization is null)
            {
                logger.LogWarning("D'avó: não foi possível capturar o token de sessão do SPA.");
                return Result<ColetaPrecoRede>.Failure("Falha ao autenticar no site do D'avó.");
            }

            var termo = string.IsNullOrWhiteSpace(nomeProduto) ? ean : nomeProduto;

            // Chama a API de busca DIRETO com o token capturado, usando o
            // CD de Suzano no path — o preço retornado é o da loja de Suzano.
            var url = $"https://services.vipcommerce.com.br/api-admin/v1/org/399/filial/1/centro_distribuicao/{CdSuzano}/loja/buscas/produtos/termo/{Uri.EscapeDataString(termo)}?page=1&session={sessaoId}";
            var resposta = await page.EvaluateAsync<string?>(
                @"async ([url, auth, sessao]) => {
                    try {
                        const r = await fetch(url, { headers: {
                            'authorization': auth,
                            'domainkey': 'davo.com.br',
                            'organizationid': '399',
                            'sessao-id': sessao ?? '',
                            'accept': 'application/json'
                        }});
                        const corpo = await r.text();
                        return r.status + '||' + corpo;
                    } catch (e) { return 'ERR||' + e; }
                }", new[] { url, authorization, sessaoId ?? "" });

            var json = resposta;
            if (resposta is not null)
            {
                var sep = resposta.IndexOf("||", StringComparison.Ordinal);
                var status = sep >= 0 ? resposta[..sep] : "?";
                json = sep >= 0 ? resposta[(sep + 2)..] : resposta;
                logger.LogInformation("D'avó: API de busca respondeu {Status} para '{Termo}'.", status, termo);
                if (status != "200")
                {
                    logger.LogWarning("D'avó: corpo da resposta: {Corpo}", json?.Length > 300 ? json[..300] : json);
                    json = null;
                }
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

            // PRIORIDADE 1: match exato de EAN — a API expõe codigo_barras,
            // então quando o EAN confere não há ambiguidade (evita escolher
            // um kit/variante com score de tokens maior).
            var porEan = itens.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(ean)
                && string.Equals(x.CodigoBarras?.Trim(), ean.Trim(), StringComparison.OrdinalIgnoreCase));

            // PRIORIDADE 2: melhor score de tokens, exigindo os tokens
            // obrigatórios (medidas como "2l" e marca como "coca").
            var porScore = itens
                .Select(x => (Item: x, Score: NomeProdutoMatcher.Pontuar(termo, x.Descricao)))
                .Where(x => x.Score >= NomeProdutoMatcher.ScoreMinimo
                    && NomeProdutoMatcher.ContemTokensObrigatorios(termo, x.Item.Descricao))
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            var melhorItem = porEan ?? porScore.Item;

            if (melhorItem is null)
            {
                logger.LogWarning("D'avó: nenhum resultado casou com o EAN ou os tokens obrigatórios de '{Termo}'.", termo);
                return Result<ColetaPrecoRede>.Success(
                    new ColetaPrecoRede(Rede, null, null, Disponivel: false));
            }

            // Preço de oferta (quando em_oferta) tem prioridade — é o
            // preço em destaque no site.
            var preco = melhorItem.PrecoOferta ?? melhorItem.Preco;
            var disponivel = melhorItem.Disponivel && preco is not null;
            // A URL real do site é /produto/{produto_id}/{slug} — o campo
            // "link" da API traz SÓ o slug; sem o id o SPA redireciona
            // para /produto/NaN/{slug} e exibe "Produto Indisponível".
            var urlProduto = melhorItem.Link is not null && melhorItem.ProdutoId is not null
                ? $"https://www.davo.com.br/produto/{melhorItem.ProdutoId}/{melhorItem.Link}"
                : null;

            // A API expõe o EAN: confiança High quando confere, Medium só
            // por nome. Região CONFIRMADA (CD de Suzano selecionado).
            var confirmadoPorEan = !string.IsNullOrWhiteSpace(ean)
                && string.Equals(melhorItem.CodigoBarras?.Trim(), ean.Trim(), StringComparison.OrdinalIgnoreCase);
            var confianca = preco is null
                ? NivelConfiancaColeta.Unverified
                : confirmadoPorEan ? NivelConfiancaColeta.High : NivelConfiancaColeta.Medium;

            return Result<ColetaPrecoRede>.Success(new ColetaPrecoRede(
                Rede, melhorItem.Descricao, preco, disponivel, urlProduto,
                TipoPrecoColeta.Normal, confianca, RegiaoConfirmada: true));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "D'avó: falha ao coletar o EAN {Ean} via Playwright.", ean);
            return Result<ColetaPrecoRede>.Failure("Falha ao consultar o site do D'avó.");
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
                    ProdutoId = p.TryGetProperty("produto_id", out var pid) && pid.ValueKind == System.Text.Json.JsonValueKind.Number
                        ? pid.GetInt64()
                        : null,
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
        public long? ProdutoId { get; set; }
        public bool Disponivel { get; set; }
        public decimal? Preco { get; set; }
        public decimal? PrecoOferta { get; set; }
    }
}
