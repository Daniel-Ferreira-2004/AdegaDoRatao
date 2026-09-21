using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AdegaDoRatao.Infrastructure.ExternalServices.Coletores;

/// <summary>
/// Compara o nome buscado com o nome do produto retornado pelo site da
/// rede, por tokens (palavras). A busca não precisa ser exata: basta que
/// os tokens significativos da busca apareçam no nome do candidato
/// (ex.: "Doritos 120g" casa com "Salgadinho Elma Chips Doritos Queijo
/// Nacho 120g"). Tokens com menos de 2 caracteres são ignorados.
/// </summary>
internal static class NomeProdutoMatcher
{
    /// <summary>Score mínimo (0 a 1) para aceitar um candidato sem confirmação de EAN.</summary>
    public const double ScoreMinimo = 0.5;

    /// <summary>
    /// Pontuação de 0 a 1: fração dos tokens da busca presentes no nome
    /// do candidato. Retorna 0 quando a busca não tem tokens válidos.
    /// </summary>
    public static double Pontuar(string? termoBuscado, string? nomeCandidato)
    {
        if (string.IsNullOrWhiteSpace(termoBuscado) || string.IsNullOrWhiteSpace(nomeCandidato))
        {
            return 0;
        }

        var tokensBusca = Tokens(termoBuscado);
        if (tokensBusca.Count == 0)
        {
            return 0;
        }

        var nomeNormalizado = Normalizar(nomeCandidato);
        var encontrados = tokensBusca.Count(nomeNormalizado.Contains);
        return (double)encontrados / tokensBusca.Count;
    }

    /// <summary>
    /// Verifica se o candidato contém os tokens OBRIGATÓRIOS da busca:
    /// todos os tokens com dígitos (medidas como "2l", "700g", "120")
    /// e pelo menos um token alfabético significativo (ex.: "coca",
    /// "nutella"). Evita casar "Coca-Cola 2L" com "Coca-Cola 350ml".
    /// </summary>
    public static bool ContemTokensObrigatorios(string? termoBuscado, string? nomeCandidato)
    {
        if (string.IsNullOrWhiteSpace(termoBuscado) || string.IsNullOrWhiteSpace(nomeCandidato))
        {
            return false;
        }

        var tokens = Tokens(termoBuscado);
        var nome = Normalizar(nomeCandidato);
        var comDigito = tokens.Where(t => t.Any(char.IsDigit)).ToList();
        var alfabeticos = tokens.Where(t => t.Length >= 3 && t.All(char.IsLetter)).ToList();
        return comDigito.All(nome.Contains)
            && (alfabeticos.Count == 0 || alfabeticos.Any(nome.Contains));
    }

    private static List<string> Tokens(string texto)
        => Regex.Split(Normalizar(texto), @"[^a-z0-9]+")
            .SelectMany(t =>
            {
                // Mantém o token composto ("2l", "700g") e também suas
                // partes ("2", "700") para casar com "2 l" / "700 g".
                // Tokens com dígito são mantidos mesmo com 1 caractere.
                var partes = Regex.Split(t, @"(?<=\d)(?=[a-z])|(?<=[a-z])(?=\d)")
                    .Where(p => p.Length >= 2 || p.Any(char.IsDigit));
                return partes.Append(t);
            })
            .Where(t => (t.Length >= 2 || t.Any(char.IsDigit))
                && t is not "ml" and not "gr" and not "kg" and not "un")
            .Distinct()
            .ToList();

    private static string Normalizar(string texto)
    {
        var decomposto = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposto.Length);
        foreach (var c in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
