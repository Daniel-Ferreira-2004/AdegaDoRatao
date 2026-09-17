using AdegaDoRatao.Application.Common;
using FluentAssertions;
using Xunit;

namespace AdegaDoRatao.UnitTests.Application;

/// <summary>
/// Testes do cálculo de Haversine contra distâncias reais conhecidas.
/// Referência: Suzano/SP (centro) → São Paulo/SP (Praça da Sé) ≈ 33 km
/// em linha reta; 1° de latitude ≈ 111,2 km em qualquer ponto do globo.
/// </summary>
public class GeoDistanceTests
{
    [Fact]
    public void CalcularKm_MesmoPonto_DeveRetornarZero()
    {
        var distancia = GeoDistance.CalcularKm(-23.5425, -46.3108, -23.5425, -46.3108);

        distancia.Should().Be(0);
    }

    [Fact]
    public void CalcularKm_SuzanoAteSaoPaulo_DeveRetornarDistanciaRealConhecida()
    {
        // Centro de Suzano/SP → Praça da Sé, São Paulo/SP.
        // Distância em linha reta medida em ferramentas de mapa: ~33 km.
        var distancia = GeoDistance.CalcularKm(-23.5425, -46.3108, -23.5505, -46.6333);

        distancia.Should().BeInRange(32.0, 34.0);
    }

    [Fact]
    public void CalcularKm_UmGrauDeLatitude_DeveRetornarAproximadamente111Km()
    {
        var distancia = GeoDistance.CalcularKm(0, 0, 1, 0);

        distancia.Should().BeApproximately(111.19, 0.5);
    }

    [Fact]
    public void CalcularKm_DeveSerSimetrico()
    {
        var ida = GeoDistance.CalcularKm(-23.5425, -46.3108, -23.5505, -46.6333);
        var volta = GeoDistance.CalcularKm(-23.5505, -46.6333, -23.5425, -46.3108);

        volta.Should().BeApproximately(ida, 0.0001);
    }
}
