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
    public async Task ConsultarAsync_ComLojasEncontradas_DeveRetornarTodasSemFiltroDeRede()
    {
        var produto = CriarProduto();
        _products.Setup(x => x.ObterPorIdAsync(produto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(produto);
        IReadOnlyList<PrecoMercadoRedeResponse> precos = new PrecoMercadoRedeResponse[]
        {
            new("Oxan Atacadista", "São Paulo", 7.49m, true, null),
            new("Super Tonin", "São Sebastião do Paraíso", 7.99m, true, null),
            new("Loja Sem Preço", "Suzano", null, false, "Preço indisponível nesta loja."),
        };
        _externo.Setup(x => x.ConsultarPrecosAsync(produto.Barcode!, It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConsultaExternaPrecos>.Success(new ConsultaExternaPrecos(precos, OrigemCache: false)));
        var service = CriarServico();

        var response = await service.ConsultarAsync(produto.Id);

        response.ConsultaFalhou.Should().BeFalse();
        response.Ean.Should().Be(produto.Barcode);
        response.Precos.Should().HaveCount(3);
        response.Precos.Single(p => p.Rede == "Oxan Atacadista").Preco.Should().Be(7.49m);
        response.Precos.Single(p => p.Rede == "Loja Sem Preço").Disponivel.Should().BeFalse();
    }
}
