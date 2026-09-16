using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Exceptions;
using AdegaDoRatao.Domain.Interfaces;

namespace AdegaDoRatao.Application.Services;

/// <summary>Casos de uso de produto. As verificações que consultam dados ficam aqui.</summary>
public sealed class ProductService : IProductService
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IBrandRepository _brands;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _audit;

    public ProductService(IProductRepository products, ICategoryRepository categories, IBrandRepository brands,
        IUnitOfWork unitOfWork, IAuditService audit)
        => (_products, _categories, _brands, _unitOfWork, _audit) = (products, categories, brands, unitOfWork, audit);

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (await _products.SkuJaExisteAsync(request.Sku, null, cancellationToken))
            throw new UseCaseException("Já existe um produto cadastrado com este SKU.");
        if (!string.IsNullOrWhiteSpace(request.Barcode) && await _products.CodigoDeBarrasJaExisteAsync(request.Barcode, null, cancellationToken))
            throw new UseCaseException("Já existe um produto cadastrado com este código de barras.");

        await EnsureCatalogAsync(request.CategoryId, request.BrandId, cancellationToken);
        var product = new Product(request.Name, request.Description, request.Sku, request.Barcode, request.CategoryId,
            request.BrandId, request.UnitOfMeasure, request.CostPrice, request.SalePrice, request.MinStock,
            request.MaxStock, request.AllowNegativeStock);
        await _products.AdicionarAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.RegisterAsync("CREATE", nameof(Product), product.Id.ToString(), null, ToResponse(product), cancellationToken);
        return ToResponse(product);
    }

    public async Task<ProductResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => ToResponse(await GetRequiredAsync(id, cancellationToken));

    public async Task<PagedResult<ProductResponse>> SearchAsync(ProductQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var products = await _products.PesquisarAsync(query.Name, query.Sku, query.CategoryId, query.IsActive, page, pageSize, cancellationToken);
        // O contrato atual de repositório não expõe COUNT; a Infrastructure poderá acrescentá-lo
        // sem mudar este DTO. Até lá, o total representa a quantidade retornada nesta busca.
        return new PagedResult<ProductResponse>(products.Select(ToResponse).ToArray(), page, pageSize, products.Count);
    }

    public async Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await GetRequiredAsync(id, cancellationToken);
        if (!string.IsNullOrWhiteSpace(request.Barcode) && await _products.CodigoDeBarrasJaExisteAsync(request.Barcode, id, cancellationToken))
            throw new UseCaseException("Já existe um produto cadastrado com este código de barras.");
        await EnsureCatalogAsync(request.CategoryId, request.BrandId, cancellationToken);
        product.AtualizarDadosCadastrais(request.Name, request.Description, request.UnitOfMeasure, request.CategoryId, request.BrandId);
        product.AlterarCodigoDeBarras(request.Barcode);
        product.AlterarLimitesDeEstoque(request.MinStock, request.MaxStock);
        product.PermitirEstoqueNegativo(request.AllowNegativeStock);
        _products.Atualizar(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.RegisterAsync("UPDATE", nameof(Product), id.ToString(), null, ToResponse(product), cancellationToken);
        return ToResponse(product);
    }

    public async Task<ProductResponse> ChangePricesAsync(Guid id, ChangeProductPricesRequest request, CancellationToken cancellationToken = default)
    {
        var product = await GetRequiredAsync(id, cancellationToken);
        var before = new { product.CostPrice, product.SalePrice };
        product.AlterarPrecos(request.CostPrice, request.SalePrice);
        _products.Atualizar(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.RegisterAsync("PRICE_CHANGE", nameof(Product), id.ToString(), before,
            new { product.CostPrice, product.SalePrice }, cancellationToken);
        return ToResponse(product);
    }

    public async Task SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default)
    {
        var product = await GetRequiredAsync(id, cancellationToken);
        if (active) product.Ativar(); else product.Desativar();
        _products.Atualizar(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.RegisterAsync(active ? "ACTIVATE" : "DEACTIVATE", nameof(Product), id.ToString(), null,
            new { product.IsActive }, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductResponse>> GetLowStockAsync(bool includeOutOfStock, CancellationToken cancellationToken = default)
    {
        var products = await _products.ListarComEstoqueBaixoAsync(cancellationToken);
        if (includeOutOfStock) products = products.Concat(await _products.ListarSemEstoqueAsync(cancellationToken)).ToArray();
        return products.OrderBy(x => x.Name).Select(ToResponse).ToArray();
    }

    private async Task<Product> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
        => await _products.ObterPorIdAsync(id, cancellationToken) ?? throw new UseCaseException("Produto não encontrado.");

    private async Task EnsureCatalogAsync(Guid categoryId, Guid brandId, CancellationToken cancellationToken)
    {
        var category = await _categories.ObterPorIdAsync(categoryId, cancellationToken)
            ?? throw new UseCaseException("Categoria não encontrada.");
        if (!category.IsActive) throw new CategoriaInativaException(category.Name);
        var brand = await _brands.ObterPorIdAsync(brandId, cancellationToken)
            ?? throw new UseCaseException("Marca não encontrada.");
        if (!brand.IsActive) throw new UseCaseException("A marca informada está inativa.");
    }

    private static ProductResponse ToResponse(Product p) => new(p.Id, p.Name, p.Description, p.Sku, p.Barcode,
        p.CategoryId, p.BrandId, p.UnitOfMeasure, p.CostPrice, p.SalePrice, p.CurrentStock, p.MinStock,
        p.MaxStock, p.AllowNegativeStock, p.IsActive, p.CreatedAt, p.UpdatedAt);
}
