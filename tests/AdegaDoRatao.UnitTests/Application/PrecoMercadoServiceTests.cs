using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Application.Services;
using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace AdegaDoRatao.UnitTests.Application;

/// <summary>
/// Testes do caso de uso de comparação de preços de mercado: produto sem
/// EAN, produto inexistente, falha da API externa (sem exceção) e sucesso.
/// </summary>
public class PrecoMercadoServiceTests
{
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IPrecoMercadoExternoService> _externo = new();

    private PrecoMercadoService CriarServico() => new(_products.Object, _externo.Object);

    private static Product CriarProduto(string? barcode = "7894900011517") => new(
        "Coca-Cola 2L", null, "COCA-2L", barcode, Guid.NewGuid(), Guid.NewGuid(),
        "UN", 5.00m, 8.50m, 6, null);

    [Fact]
    public async Task ConsultarAsync_ProdutoInexistente_DeveLancarUseCaseException()
    {
        _products.Setup(x => x.ObterPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        var service = CriarServico();

        var act = () => service.ConsultarAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<UseCaseException>().WithMessage("*não encontrado*");
        _externo.Verify(x => x.ConsultarPrecosAsync(It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConsultarAsync_ProdutoSemEan_DeveLancarUseCaseException()
    {
        var produto = CriarProduto(barcode: null);
        _products.Setup(x => x.ObterPorIdAsync(produto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(produto);
        var service = CriarServico();

        var act = () => service.ConsultarAsync(produto.Id);

        await act.Should().ThrowAsync<UseCaseException>().WithMessage("*código de barras*");
    }

    [Fact]
    public async Task ConsultarAsync_ApiExternaFalhou_DeveRetornarIndisponivelSemExcecao()
    {
        var produto = CriarProduto();
        _products.Setup(x => x.ObterPorIdAsync(produto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(produto);
        _externo.Setup(x => x.ConsultarPrecosAsync(produto.Barcode!, It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConsultaExternaPrecos>.Failure("A consulta excedeu o tempo limite."));
        var service = CriarServico();

        var response = await service.ConsultarAsync(produto.Id);

        response.ConsultaFalhou.Should().BeTrue();
        response.Precos.Should().BeEmpty();
    }

    [Fact]
    public async Task ConsultarAsync_ComLojasEncontradas_DeveRetornarApenasRedesPermitidas()
    {
        var produto = CriarProduto();
        _products.Setup(x => x.ObterPorIdAsync(produto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(produto);
        IReadOnlyList<PrecoMercadoRedeResponse> precos = new PrecoMercadoRedeResponse[]
        {
            new("Assaí Atacadista", "Suzano", 7.49m, true, null),
            new("Shibata Supermercados", "Suzano", 7.99m, true, null),
            new("Oxan Atacadista", "São Paulo", 6.99m, true, null),
            new("Super Tonin", "São Sebastião do Paraíso", 7.19m, true, null),
            new("Veran Suzano", "Suzano", null, false, "Preço indisponível nesta loja."),
        };
        _externo.Setup(x => x.ConsultarPrecosAsync(produto.Barcode!, It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConsultaExternaPrecos>.Success(new ConsultaExternaPrecos(precos, OrigemCache: false)));
        var service = CriarServico();

        var response = await service.ConsultarAsync(produto.Id);

        response.ConsultaFalhou.Should().BeFalse();
        response.Ean.Should().Be(produto.Barcode);
        // Oxan e Super Tonin não estão na lista de redes permitidas.
        response.Precos.Select(p => p.Rede).Should().BeEquivalentTo(
            "Assaí Atacadista", "Shibata Supermercados", "Veran Suzano");
        response.Precos.Single(p => p.Rede == "Assaí Atacadista").Preco.Should().Be(7.49m);
        response.Precos.Single(p => p.Rede == "Veran Suzano").Disponivel.Should().BeFalse();
    }

    [Theory]
    [InlineData("Sonda Supermercados", true)]
    [InlineData("Nagumo", true)]
    [InlineData("Atacadão Dia a Dia", true)]
    [InlineData("Tenda Atacado", true)]
    [InlineData("Rossi", true)]
    [InlineData("Extra Hiper", true)]
    [InlineData("Semar", true)]
    [InlineData("D'Avó Supermercados", true)]
    [InlineData("Soni", true)]
    [InlineData("Carrefour", false)]
    [InlineData("Pão de Açúcar", false)]
    public async Task ConsultarAsync_FiltroDeRedes_DeveRespeitarListaPermitida(string rede, bool esperado)
    {
        var produto = CriarProduto();
        _products.Setup(x => x.ObterPorIdAsync(produto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(produto);
        IReadOnlyList<PrecoMercadoRedeResponse> precos = new PrecoMercadoRedeResponse[]
        {
            new(rede, "Suzano", 7.49m, true, null),
        };
        _externo.Setup(x => x.ConsultarPrecosAsync(produto.Barcode!, It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConsultaExternaPrecos>.Success(new ConsultaExternaPrecos(precos, OrigemCache: false)));
        var service = CriarServico();

        var response = await service.ConsultarAsync(produto.Id);

        response.Precos.Any().Should().Be(esperado);
    }
}
