using System.Text.Json;
using System.Text.Json.Serialization;
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
/// O EAN não vem em campo próprio — aparece no nome do arquivo da imagem
/// (thumbnail), ex.: ".../7894900027013_oficial-....jpg".
///
/// O preço exibido já considera a loja/região padrão do site; o CEP da
/// adega (08503-000) é atendido pela loja de Guarulhos.
/// </summary>
public sealed partial class TendaPrecoCollector(
    HttpClient http,
    ILogger<TendaPrecoCollector> logger) : IPrecoRedeCollector
{
    public string Rede => "Tenda";

    public async Task<Result<ColetaPrecoRede>> ColetarAsync(string ean, CancellationToken cancellationToken = default)
    {
        try
        {
            // Busca pelo próprio EAN — o Tenda indexa EANs na busca textual.
            var html = await http.GetStringAsync($"busca?q={Uri.EscapeDataString(ean)}", cancellationToken);

            var match = NextDataRegex().Match(html);
            if (!match.Success)
            {
                logger.LogWarning("Tenda: bloco __NEXT_DATA__ não encontrado para o EAN {Ean}.", ean);
                return Result<ColetaPrecoRede>.Failure("Estrutura da página do Tenda não reconhecida.");
            }

            using var doc = JsonDocument.Parse(match.Groups[1].Value);
            var produto = EncontrarProdutoPorEan(doc.RootElement, ean);
            if (produto is null)
            {
                return Result<ColetaPrecoRede>.Success(
                    new ColetaPrecoRede(Rede, null, null, Disponivel: false));
            }

            return Result<ColetaPrecoRede>.Success(new ColetaPrecoRede(
                Rede,
                produto.Value.Nome,
                produto.Value.Preco,
                produto.Value.Disponivel));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Tenda: falha ao coletar o EAN {Ean}.", ean);
            return Result<ColetaPrecoRede>.Failure("Falha ao consultar o site do Tenda.");
        }
    }

    private static (string Nome, decimal? Preco, bool Disponivel)? EncontrarProdutoPorEan(
        JsonElement root, string ean)
    {
        // Os produtos ficam em props.pageProps... — percorremos a árvore
        // procurando objetos com "thumbnail" contendo o EAN.
        var stack = new Stack<JsonElement>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var atual = stack.Pop();
            switch (atual.ValueKind)
            {
                case JsonValueKind.Object:
                    if (atual.TryGetProperty("thumbnail", out var thumb)
                        && thumb.ValueKind == JsonValueKind.String
                        && (thumb.GetString()?.Contains(ean) ?? false))
                    {
                        var nome = atual.TryGetProperty("name", out var n) ? n.GetString() : null;
                        decimal? preco = null;
                        if (atual.TryGetProperty("price", out var p) && p.ValueKind == JsonValueKind.Number)
                        {
                            preco = p.GetDecimal();
                        }

                        var disponivel = atual.TryGetProperty("isAvailable", out var a)
                            && a.ValueKind == JsonValueKind.True;

                        return (nome ?? string.Empty, preco, disponivel && preco is not null);
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

        return null;
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
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36");
    }
}
