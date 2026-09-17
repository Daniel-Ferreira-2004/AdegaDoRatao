using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Application.Services;
using AdegaDoRatao.Application.Validators;
using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace AdegaDoRatao.UnitTests.Application;

/// <summary>
/// Testes do caso de uso de mercados próximos: raio sem resultados,
/// ordenação por distância, filtro pelo raio informado, validação de
/// parâmetros e geocodificação por endereço (mockada — nunca bate na
/// API externa real).
/// </summary>
public class MercadoServiceTests
{
    // Ponto de referência: centro de Suzano/SP.
    private const double LatBase = -23.5425;
    private const double LngBase = -46.3108;

    private readonly Mock<IMarketRepository> _markets = new();
    private readonly Mock<IGeocodingService> _geocoding = new();

    private MercadoService CriarServico() => new(
        _markets.Object, _geocoding.Object, new BuscarMercadosProximosQueryValidator());

    private static Market CriarMercado(string nome, string rede, double lat, double lng) =>
        new(nome, rede, "Rua Exemplo, 100", "Suzano", "SP", "08600-000", lat, lng);

    [Fact]
    public async Task BuscarProximosAsync_SemMercadosNoRaio_DeveRetornarListaVazia()
    {
        // Mercado a ~33 km (São Paulo), fora do raio de 5 km.
        _markets.Setup(x => x.ListarAtivosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CriarMercado("Atacadão SP", "Atacadão", -23.5505, -46.6333) });
        var service = CriarServico();

        var resultado = await service.BuscarProximosAsync(new BuscarMercadosProximosQuery(LatBase, LngBase, 5));

        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task BuscarProximosAsync_DeveOrdenarDoMaisProximoAoMaisDistante()
    {
        _markets.Setup(x => x.ListarAtivosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                // ~2,2 km do ponto base
                CriarMercado("Shibata Jardim", "Shibata", LatBase + 0.02, LngBase),
                // ~0,1 km do ponto base (o mais próximo)
                CriarMercado("Veran Centro", "Veran", LatBase + 0.001, LngBase),
                // ~4,4 km do ponto base
                CriarMercado("Semar Longe", "Semar", LatBase + 0.04, LngBase),
            });
        var service = CriarServico();

        var resultado = await service.BuscarProximosAsync(new BuscarMercadosProximosQuery(LatBase, LngBase, 5));

        resultado.Select(x => x.Nome).Should().Equal("Veran Centro", "Shibata Jardim", "Semar Longe");
        resultado.Select(x => x.DistanciaKm).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task BuscarProximosAsync_DeveRespeitarORaioInformado()
    {
        _markets.Setup(x => x.ListarAtivosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                CriarMercado("Veran Centro", "Veran", LatBase + 0.001, LngBase), // ~0,1 km
                CriarMercado("Shibata Jardim", "Shibata", LatBase + 0.02, LngBase), // ~2,2 km
            });
        var service = CriarServico();

        // Raio de 1 km: só o mercado mais próximo entra.
        var resultado = await service.BuscarProximosAsync(new BuscarMercadosProximosQuery(LatBase, LngBase, 1));

        resultado.Should().ContainSingle();
        resultado[0].Nome.Should().Be("Veran Centro");
        resultado[0].DistanciaKm.Should().BeLessThanOrEqualTo(1);
    }

    [Theory]
    [InlineData(0.1)]   // abaixo do mínimo (0,5 km)
    [InlineData(25.0)]  // acima do máximo (20 km)
    public async Task BuscarProximosAsync_RaioForaDosLimites_DeveLancarUseCaseException(double raioKm)
    {
        var service = CriarServico();

        var act = () => service.BuscarProximosAsync(new BuscarMercadosProximosQuery(LatBase, LngBase, raioKm));

        await act.Should().ThrowAsync<UseCaseException>().WithMessage("*raio*");
        _markets.Verify(x => x.ListarAtivosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BuscarProximosPorEnderecoAsync_EnderecoValido_DeveGeocodificarEBuscar()
    {
        _geocoding.Setup(x => x.GeocodificarAsync("08600-000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Coordenadas>.Success(new Coordenadas(LatBase, LngBase)));
        _markets.Setup(x => x.ListarAtivosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CriarMercado("Veran Centro", "Veran", LatBase + 0.001, LngBase) });
        var service = CriarServico();

        var resultado = await service.BuscarProximosPorEnderecoAsync("08600-000");

        resultado.Should().ContainSingle();
        resultado[0].Nome.Should().Be("Veran Centro");
    }

    [Fact]
    public async Task BuscarProximosPorEnderecoAsync_EnderecoNaoEncontrado_DeveLancarUseCaseExceptionComMensagemClara()
    {
        _geocoding.Setup(x => x.GeocodificarAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Coordenadas>.Failure(
                "Endereço não encontrado. Verifique o endereço ou CEP informado e tente novamente."));
        var service = CriarServico();

        var act = () => service.BuscarProximosPorEnderecoAsync("endereço inexistente xyz");

        await act.Should().ThrowAsync<UseCaseException>().WithMessage("*não encontrado*");
        _markets.Verify(x => x.ListarAtivosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BuscarProximosPorEnderecoAsync_EnderecoVazio_DeveLancarUseCaseExceptionSemChamarGeocoding()
    {
        var service = CriarServico();

        var act = () => service.BuscarProximosPorEnderecoAsync("   ");

        await act.Should().ThrowAsync<UseCaseException>().WithMessage("*endereço*");
        _geocoding.Verify(x => x.GeocodificarAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
