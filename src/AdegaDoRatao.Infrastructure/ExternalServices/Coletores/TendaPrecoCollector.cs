using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AdegaDoRatao.Infrastructure.ExternalServices.Coletores;

/// <summary>
/// Coletor de preços do Tenda Atacado (tendaatacado.com.br).
///
/// O site é Next.js e embute os resultados da busca no bloco
/// &lt;script id="__NEXT_DATA__"&gt; da página /busca?q={termo}.
/// A API VTEX pública NÃO está habilitada nesta conta (404), por isso
/// a coleta é feita pelo HTML da página de busca.
///
/// A busca é feita pelo NOME do produto (mais confiável que EAN). O
/// resultado é confirmado pelo EAN quando ele aparece na thumbnail do
/// produto; senão, vale o candidato com melhor score de tokens
/// (<see cref="NomeProdutoMatcher"/>).
///
/// REGIONALIZAÇÃO (mapeada por engenharia reversa em 2026-09-23): o Tenda
/// regionaliza o preço por FILIAL — sem sessão, o site responde com os
/// preços da filial padrão (Guarulhos). Para obter o preço da filial que
/// atende a adega (CEP 08533-020 → Ferraz de Vasconcelos / Itaquaquecetuba),
/// o coletor cria uma SESSÃO ANÔNIMA na API pública (o mesmo fluxo do
/// front-end):
///   1) POST api/public/anonymous-client → credenciais {user, password}
///   2) POST api/public/oauth/access-token → access_token (X-Authorization)
///   3) GET  api/client → id do cliente (cookie _Tendaatacado-userInfo)
///   4) POST api/shopping-cart {"zipcode":8533020} → cartID + branchId
/// Os cookies _Tendaatacado-* resultantes regionalizam o HTML da busca.
/// Se a sessão falhar, coleta SEM regionalização (RegiaoConfirmada=false).
/// </summary>
public sealed partial class TendaPrecoCollector(
    HttpClient http,
    ILogger<TendaPrecoCollector> logger) : IPrecoRedeCollector
{
    public string Rede => "Tenda";

    public async Task<Result<ColetaPrecoRede>> ColetarAsync(string ean, string? nomeProduto = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var termo = string.IsNullOrWhiteSpace(nomeProduto) ? ean : nomeProduto;

            // Sessão regionalizada (CEP da adega). Quando disponível, o
            // preço é da filial de Ferraz de Vasconcelos — confirmada.
            var cookieRegional = await ObterCookieRegionalAsync(cancellationToken);

            using var requisicao = new HttpRequestMessage(HttpMethod.Get,
                $"busca?q={Uri.EscapeDataString(termo)}");
            if (cookieRegional is not null)
            {
                requisicao.Headers.TryAddWithoutValidation("Cookie", cookieRegional);
            }

            using var resposta = await http.SendAsync(requisicao, cancellationToken);
            resposta.EnsureSuccessStatusCode();
            var html = await resposta.Content.ReadAsStringAsync(cancellationToken);

            var match = NextDataRegex().Match(html);
            if (!match.Success)
            {
                logger.LogWarning("Tenda: bloco __NEXT_DATA__ não encontrado para '{Termo}'.", termo);
                return Result<ColetaPrecoRede>.Failure("Estrutura da página do Tenda não reconhecida.");
            }

            using var doc = JsonDocument.Parse(match.Groups[1].Value);
            var produto = EncontrarProduto(doc.RootElement, ean, nomeProduto);
            if (produto is null)
            {
                return Result<ColetaPrecoRede>.Success(
                    new ColetaPrecoRede(Rede, null, null, Disponivel: false));
            }

            // Se o JSON não trouxe a URL do produto, usa a página de busca
            // como fallback (o usuário ainda vê o produto no site).
            var url = produto.Value.Url
                ?? $"https://www.tendaatacado.com.br/busca?q={Uri.EscapeDataString(termo)}";

            // Região: confirmada quando a sessão regionalizada está ativa
            // (CEP 08533-020 → filial Ferraz/Itaquaquecetuba); sem sessão,
            // o preço é da filial padrão (Guarulhos) — NÃO confirmada.
            var regiaoConfirmada = cookieRegional is not null;
            var confianca = produto.Value.Preco is null
                ? NivelConfiancaColeta.Unverified
                : (produto.Value.EanConfirmado, regiaoConfirmada) switch
                {
                    (true, true) => NivelConfiancaColeta.High,
                    (true, false) => NivelConfiancaColeta.Medium,
                    (false, true) => NivelConfiancaColeta.Medium,
                    _ => NivelConfiancaColeta.Low
                };

            return Result<ColetaPrecoRede>.Success(new ColetaPrecoRede(
                Rede,
                produto.Value.Nome,
                produto.Value.Preco,
                produto.Value.Disponivel,
                url,
                produto.Value.Tipo,
                confianca,
                regiaoConfirmada));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Tenda: falha ao coletar o EAN {Ean}.", ean);
            return Result<ColetaPrecoRede>.Failure("Falha ao consultar o site do Tenda.");
        }
    }

    private static (string Nome, decimal? Preco, bool Disponivel, string? Url, bool EanConfirmado, TipoPrecoColeta Tipo)? EncontrarProduto(
        JsonElement root, string ean, string? nomeProduto)
    {
        // Percorre a árvore JSON procurando objetos de produto (com "name"
        // e "price"). Prioridade: 1) EAN na thumbnail; 2) melhor score de
        // tokens do nome; 3) nada → indisponível.
        (string Nome, decimal? Preco, bool Disponivel, string? Url, bool EanConfirmado, TipoPrecoColeta Tipo)? melhor = null;
        var melhorScore = 0.0;
        var stack = new Stack<JsonElement>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var atual = stack.Pop();
            switch (atual.ValueKind)
            {
                case JsonValueKind.Object:
                    if (atual.TryGetProperty("name", out var n)
                        && atual.TryGetProperty("price", out var p)
                        && p.ValueKind == JsonValueKind.Number)
                    {
                        var nome = n.GetString() ?? string.Empty;
                        var preco = p.GetDecimal();
                        var disponivel = atual.TryGetProperty("isAvailable", out var a)
                            && a.ValueKind == JsonValueKind.True;

                        // Desconto por quantidade: o Tenda expõe
                        // "wholesalePrices":[{"minQuantity":6,"price":4.79}]
                        // — o preço em destaque no site ("a partir de N
                        // unidades"). Quando menor que o unitário, devolvemos
                        // ESSE preço, marcado como CondicionadoQuantidade.
                        var tipo = TipoPrecoColeta.Normal;
                        if (atual.TryGetProperty("wholesalePrices", out var wp)
                            && wp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var tier in wp.EnumerateArray())
                            {
                                if (tier.TryGetProperty("price", out var tp)
                                    && tp.ValueKind == JsonValueKind.Number
                                    && tp.GetDecimal() is var precoTier
                                    && precoTier > 0 && precoTier < preco)
                                {
                                    preco = precoTier;
                                    tipo = TipoPrecoColeta.CondicionadoQuantidade;
                                }
                            }
                        }

                        // URL do produto: o Next.js costuma expor "url" ou
                        // "slug" no objeto do produto.
                        string? url = null;
                        if (atual.TryGetProperty("url", out var u) && u.ValueKind == JsonValueKind.String)
                        {
                            url = u.GetString();
                        }
                        else if (atual.TryGetProperty("slug", out var s) && s.ValueKind == JsonValueKind.String)
                        {
                            url = $"https://www.tendaatacado.com.br/produto/{s.GetString()}";
                        }

                        // O EAN na thumbnail NÃO basta sozinho: o Tenda
                        // reutiliza imagens entre variantes (ex.: o leite
                        // condensado Zero Lactose usa a foto do tradicional,
                        // com o MESMO EAN no nome do arquivo). Exige também
                        // os tokens obrigatórios — que rejeitam variantes
                        // ("zero", "diet"...) ausentes na busca.
                        var thumbConfirma = atual.TryGetProperty("thumbnail", out var thumb)
                            && thumb.ValueKind == JsonValueKind.String
                            && (thumb.GetString()?.Contains(ean) ?? false)
                            && NomeProdutoMatcher.ContemTokensObrigatorios(nomeProduto, nome);

                        var candidato = (nome, (decimal?)preco, disponivel, url, thumbConfirma, tipo);

                        if (thumbConfirma)
                        {
                            return candidato;
                        }

                        var score = NomeProdutoMatcher.Pontuar(nomeProduto, nome);
                        if (score >= NomeProdutoMatcher.ScoreMinimo && score > melhorScore
                            && NomeProdutoMatcher.ContemTokensObrigatorios(nomeProduto, nome))
                        {
                            melhor = candidato;
                            melhorScore = score;
                        }
                    }

                    foreach (var prop in atual.EnumerateObject())
                    {
                        stack.Push(prop.Value);
                    }
                    break;

                case JsonValueKind.Array:
                    foreach (var item in atual.EnumerateArray())
                    {
                        stack.Push(item);
                    }
                    break;
            }
        }

        return melhor;
    }

    [GeneratedRegex("<script id=\"__NEXT_DATA__\" type=\"application/json\">(.*?)</script>", RegexOptions.Singleline)]
    private static partial Regex NextDataRegex();

    // ---- Regionalização (CEP da adega → filial Ferraz de Vasconcelos) ----

    private const string CepAdega = "08533020";
    private const string ApiBase = "https://api.tendaatacado.com.br";

    // Credenciais PÚBLICAS do front-end do Tenda (embutidas no JS do site,
    // usadas para sessões anônimas — não são segredo da adega).
    private const string ClientId = "79ggnm96dwlojly6mqulzval0h4b94gc";
    private const string ClientSecret = "ix2tid1exrsvc8u4ta2tys1p495sa3sk3h6o6fgp0kdpu7xgmb595b8525m9rfvj";

    // Cache da sessão (o coletor é resolvido uma vez por execução do job;
    // a sessão anônima dura ~3h, renovamos a cada 2h por segurança).
    private string? _cookieRegional;
    private DateTime _cookieExpira = DateTime.MinValue;
    private readonly SemaphoreSlim _sessaoLock = new(1, 1);

    /// <summary>
    /// Cria (ou reutiliza) a sessão anônima regionalizada no CEP da adega
    /// e devolve o header Cookie com os _Tendaatacado-*. Retorna null quando
    /// a sessão falha — a coleta segue SEM regionalização (nunca quebra).
    /// </summary>
    private async Task<string?> ObterCookieRegionalAsync(CancellationToken cancellationToken)
    {
        if (_cookieRegional is not null && DateTime.UtcNow < _cookieExpira)
        {
            return _cookieRegional;
        }

        await _sessaoLock.WaitAsync(cancellationToken);
        try
        {
            if (_cookieRegional is not null && DateTime.UtcNow < _cookieExpira)
            {
                return _cookieRegional;
            }

            // 1) Cliente anônimo → credenciais temporárias.
            var anon = await (await http.PostAsJsonAsync(
                    $"{ApiBase}/api/public/anonymous-client", new { }, cancellationToken))
                .Content.ReadFromJsonAsync<TendaAnonimo>(cancellationToken: cancellationToken);
            if (anon?.User is null || anon.Password is null)
            {
                return null;
            }

            // 2) Token OAuth (grant password com as credenciais anônimas).
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = anon.User,
                ["password"] = anon.Password,
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
                ["grant_type"] = "password"
            });
            var token = await (await http.PostAsync(
                    $"{ApiBase}/api/public/oauth/access-token?g-recaptcha-response=null", form, cancellationToken))
                .Content.ReadFromJsonAsync<TendaToken>(cancellationToken: cancellationToken);
            if (token?.AccessToken is null)
            {
                return null;
            }

            // 3) Id do cliente (cookie _Tendaatacado-userInfo).
            using var reqClient = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/api/client");
            reqClient.Headers.TryAddWithoutValidation("X-Authorization", $"Bearer {token.AccessToken}");
            var cliente = await (await http.SendAsync(reqClient, cancellationToken))
                .Content.ReadFromJsonAsync<TendaCliente>(cancellationToken: cancellationToken);

            // 4) Carrinho com o CEP da adega → cartID + branchId da filial.
            using var reqCart = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/api/shopping-cart")
            {
                Content = JsonContent.Create(new { zipcode = int.Parse(CepAdega) })
            };
            reqCart.Headers.TryAddWithoutValidation("X-Authorization", $"Bearer {token.AccessToken}");
            var carrinho = await (await http.SendAsync(reqCart, cancellationToken))
                .Content.ReadFromJsonAsync<TendaCarrinho>(cancellationToken: cancellationToken);

            if (cliente?.Id is null || carrinho?.ShoppingCartId is null || carrinho.BranchId is null)
            {
                return null;
            }

            var expiracaoMs = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn).ToUnixTimeMilliseconds();
            var authCookie = $"{{\"access_token\":\"{token.AccessToken}\",\"refresh_token\":\"{token.RefreshToken}\",\"expires_in\":{token.ExpiresIn},\"expiration_date\":{expiracaoMs}}}";

            _cookieRegional = $"_Tendaatacado-branchID={carrinho.BranchId}; _Tendaatacado-cartID={carrinho.ShoppingCartId}; _Tendaatacado-userInfo={cliente.Id}; _Tendaatacado-auth-token!={authCookie}";
            _cookieExpira = DateTime.UtcNow.AddHours(2);
            logger.LogInformation("Tenda: sessão regionalizada criada (filial {Branch}).", carrinho.BranchId);
            return _cookieRegional;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or FormatException)
        {
            logger.LogWarning(ex, "Tenda: falha ao criar a sessão regionalizada; coletando sem região.");
            return null;
        }
        finally
        {
            _sessaoLock.Release();
        }
    }

    private sealed record TendaAnonimo(
        [property: JsonPropertyName("user")] string? User,
        [property: JsonPropertyName("password")] string? Password);

    private sealed record TendaToken(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);

    private sealed record TendaCliente(
        [property: JsonPropertyName("id")] long? Id);

    private sealed record TendaCarrinho(
        [property: JsonPropertyName("shoppingCartId")] long? ShoppingCartId,
        [property: JsonPropertyName("branchId")] string? BranchId);
}

/// <summary>Configuração do HttpClient do Tenda (registrada na DI).</summary>
public static class TendaPrecoCollectorSetup
{
    public static void Configurar(HttpClient client)
    {
        client.BaseAddress = new Uri("https://www.tendaatacado.com.br/");
        client.Timeout = TimeSpan.FromSeconds(20);
        // User-Agent de navegador para não ser bloqueado.
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36");
    }
}
