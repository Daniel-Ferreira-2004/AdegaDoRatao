namespace AdegaDoRatao.Application.Common;

/// <summary>
/// Cálculo de distância em linha reta entre dois pontos geográficos pela
/// fórmula de Haversine (Terra aproximada como esfera de raio 6.371 km).
///
/// Precisão: erro típico abaixo de 0,5% — mais que suficiente para o
/// filtro de "mercados próximos" (raios de 0,5 a 20 km).
///
/// NOTA DE PERFORMANCE: o cálculo é feito em memória, em C# puro, sobre
/// os mercados já carregados do banco. Funciona em qualquer banco (não
/// depende de PostGIS). Se o catálogo de lojas crescer muito (milhares),
/// vale migrar o filtro para o banco com PostGIS (geography + operador
/// &lt;-&gt; ou ST_DistanceSphere) — ver docs/18-MERCADOS-PROXIMOS.md.
/// </summary>
public static class GeoDistance
{
    private const double RaioTerraKm = 6371.0;

    /// <summary>Distância em quilômetros entre dois pontos (lat/lng em graus decimais).</summary>
    public static double CalcularKm(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        var dLat = GrausParaRadianos(latitude2 - latitude1);
        var dLon = GrausParaRadianos(longitude2 - longitude1);

        var lat1 = GrausParaRadianos(latitude1);
        var lat2 = GrausParaRadianos(latitude2);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1) * Math.Cos(lat2)
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return RaioTerraKm * c;
    }

    private static double GrausParaRadianos(double graus) => graus * Math.PI / 180.0;
}
