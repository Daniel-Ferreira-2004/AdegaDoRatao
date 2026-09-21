using System.Text.Json;
using System.Text.RegularExpressions;
using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.Interfaces;
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
            var html = await http.GetStringAsync($"busca?q={Uri.EscapeDataString(termo)}", cancellationToken);

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

            return Result<ColetaPrecoRede>.Success(new ColetaPrecoRede(
                Rede,
                produto.Value.Nome,
                produto.Value.Preco,
                produto.Value.Disponivel,
                url));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Tenda: falha ao coletar o EAN {Ean}.", ean);
            return Result<ColetaPrecoRede>.Failure("Falha ao consultar o site do Tenda.");
        }
    }

    private static (string Nome, decimal? Preco, bool Disponivel, string? Url)? EncontrarProduto(
        JsonElement root, string ean, string? nomeProduto)
    {
        // Percorre a árvore JSON procurando objetos de produto (com "name"
        // e "price"). Prioridade: 1) EAN na thumbnail; 2) melhor score de
        // tokens do nome; 3) nada → indisponível.
        (string Nome, decimal? Preco, bool Disponivel, string? Url)? melhor = null;
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

                        var candidato = (nome, (decimal?)preco, disponivel, url);

                        var thumbConfirma = atual.TryGetProperty("thumbnail", out var thumb)
                            && thumb.ValueKind == JsonValueKind.String
                            && (thumb.GetString()?.Contains(ean) ?? false);

                        if (thumbConfirma)
                        {
                            return candidato;
                        }

                        var score = NomeProdutoMatcher.Pontuar(nomeProduto, nome);
                        if (score >= NomeProdutoMatcher.ScoreMinimo && score > melhorScore)
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
