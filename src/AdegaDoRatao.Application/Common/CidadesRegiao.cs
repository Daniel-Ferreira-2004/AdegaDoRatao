namespace AdegaDoRatao.Application.Common;

/// <summary>
/// Catálogo de coordenadas (lat/lng) das cidades da região do Alto Tietê
/// e Grande SP, usado para estimar a distância entre a adega (Ferraz de
/// Vasconcelos) e a cidade de cada loja retornada pela API de preços.
///
/// A API externa (Data Market) informa apenas a CIDADE da loja, não o
/// endereço — então a distância é até o centro da cidade, com precisão
/// suficiente para ordenar "mais perto" vs "mais longe".
///
/// Chaves normalizadas: minúsculas, sem acentos (mesmo padrão de
/// <see cref="PrecoMercadoService"/>).
/// </summary>
public static class CidadesRegiao
{
    /// <summary>Coordenadas do centro de Ferraz de Vasconcelos/SP (ponto de referência da adega).</summary>
    public const double FerrazLat = -23.5411;
    public const double FerrazLng = -46.3686;

    private static readonly Dictionary<string, (double Lat, double Lng)> Coordenadas = new()
    {
        ["ferraz de vasconcelos"] = (-23.5411, -46.3686),
        ["suzano"] = (-23.5425, -46.3108),
        ["mogi das cruzes"] = (-23.5229, -46.1883),
        ["poa"] = (-23.5323, -46.3475),
        ["itaquaquecetuba"] = (-23.4861, -46.3483),
        ["arua"] = (-23.3969, -46.3208),
        ["guarulhos"] = (-23.4538, -46.5333),
        ["sao paulo"] = (-23.5505, -46.6333),
        ["sao miguel paulista"] = (-23.4987, -46.4350),
        ["itaquera"] = (-23.5361, -46.4553),
        ["sao mateus"] = (-23.6129, -46.4750),
        ["santo andre"] = (-23.6637, -46.5383),
        ["sao bernardo do campo"] = (-23.6914, -46.5646),
        ["sao caetano do sul"] = (-23.6229, -46.5510),
        ["diadema"] = (-23.6860, -46.6228),
        ["maua"] = (-23.6677, -46.4613),
        ["ribeirao pires"] = (-23.7106, -46.4133),
        ["rio grande da serra"] = (-23.7442, -46.3983),
        ["osasco"] = (-23.5325, -46.7917),
        ["barueri"] = (-23.5106, -46.8761),
        ["carapicuiba"] = (-23.5227, -46.8350),
        ["cotia"] = (-23.6039, -46.9189),
        ["taboao da serra"] = (-23.6261, -46.7917),
        ["embu das artes"] = (-23.6489, -46.8522),
        ["itapecerica da serra"] = (-23.7169, -46.8492),
        ["santana de parnaiba"] = (-23.4442, -46.9178),
        ["jandira"] = (-23.5275, -46.9025),
        ["itaquaquecetuba "] = (-23.4861, -46.3483),
        ["guararema"] = (-23.4150, -46.0358),
        ["santa isabel"] = (-23.3156, -46.2214),
        ["arujá"] = (-23.3969, -46.3208),
        ["biritiba mirim"] = (-23.5725, -46.0386),
        ["salesopolis"] = (-23.5319, -45.8458),
    };

    /// <summary>
    /// Distância em km de Ferraz de Vasconcelos até o centro da cidade
    /// informada, ou null quando a cidade não está no catálogo.
    /// </summary>
    public static double? DistanciaDeFerrazKm(string? cidade)
    {
        if (string.IsNullOrWhiteSpace(cidade))
        {
            return null;
        }

        var chave = Normalizar(cidade);
        if (!Coordenadas.TryGetValue(chave, out var coord))
        {
            return null;
        }

        return Math.Round(GeoDistance.CalcularKm(FerrazLat, FerrazLng, coord.Lat, coord.Lng), 1);
    }

    private static string Normalizar(string texto)
    {
        var decomposto = texto.Trim().ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder(decomposto.Length);
        foreach (var c in decomposto)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
