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
/// Testes dos casos de uso de produto: unicidade de SKU (RN01), alteração
/// de preço com auditoria (RN05) e validações de catálogo (RN04).
/// </summary>
public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IBrandRepository> _brands = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAuditService> _audit = new();

    private ProductService CriarServico()
        => new(_products.Object, _categories.Object, _brands.Object, _unitOfWork.Object, _audit.Object);

    private static CreateProductRequest CriarRequestValido() => new(
        Name: "Cerveja Skol 350ml",
        Description: null,
        Barcode: "7891991000763",
        CategoryId: Guid.NewGuid(),
        BrandId: Guid.NewGuid(),
        UnitOfMeasure: "UN",
        CostPrice: 2.50m,
        SalePrice: 5.00m,
        MinStock: 10,
        MaxStock: 100);

    private void ConfigurarCatalogoValido()
    {
        _categories.Setup(x => x.ObterPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category("Cervejas"));
        _brands.Setup(x => x.ObterPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Brand("Ambev"));
    }

    [Fact]
    public async Task CreateAsync_ComCodigoDeBarrasDuplicado_DeveFalhar()
    {
        // RN01: código de barras (que também é o SKU) é único
        _products.Setup(x => x.CodigoDeBarrasJaExisteAsync("7891991000763", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = CriarServico();

        var act = () => service.CreateAsync(CriarRequestValido());

        await act.Should().ThrowAsync<UseCaseException>()
            .WithMessage("*código de barras*");
        _products.Verify(x => x.AdicionarAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ComDadosValidos_DevePersistirEAuditar()
    {
        _products.Setup(x => x.CodigoDeBarrasJaExisteAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        ConfigurarCatalogoValido();
        var service = CriarServico();

        var response = await service.CreateAsync(CriarRequestValido());

        response.Name.Should().Be("Cerveja Skol 350ml");
        response.Sku.Should().Be("7891991000763");
        response.Barcode.Should().Be("7891991000763");
        _products.Verify(x => x.AdicionarAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _audit.Verify(x => x.RegisterAsync("CREATE", nameof(Product), It.IsAny<string>(),
            null, It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangePricesAsync_DeveRegistrarAuditoriaComValoresAntigosENovos()
    {
        // RN05: alteração de preço gera auditoria com valor anterior/novo
        var product = new Product("Produto", null, "SKU-1", null, Guid.NewGuid(), Guid.NewGuid(),
            "UN", 2.00m, 5.00m, 0, null);
        _products.Setup(x => x.ObterPorIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        var service = CriarServico();

        await service.ChangePricesAsync(product.Id, new ChangeProductPricesRequest(3.00m, 7.00m));

        product.CostPrice.Should().Be(3.00m);
        product.SalePrice.Should().Be(7.00m);
        _audit.Verify(x => x.RegisterAsync("PRICE_CHANGE", nameof(Product), product.Id.ToString(),
            It.IsAny<object>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangePricesAsync_ComProdutoInexistente_DeveFalhar()
    {
        _products.Setup(x => x.ObterPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        var service = CriarServico();

        var act = () => service.ChangePricesAsync(Guid.NewGuid(), new ChangeProductPricesRequest(1m, 2m));

        await act.Should().ThrowAsync<UseCaseException>();
    }
}
