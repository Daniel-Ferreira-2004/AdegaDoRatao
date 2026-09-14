using AdegaDoRatao.Domain.Entities;

namespace AdegaDoRatao.Domain.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> ObterPorSkuAsync(string sku, CancellationToken cancellationToken = default);

    Task<Product?> ObterPorCodigoDeBarrasAsync(string barcode, CancellationToken cancellationToken = default);

    Task<bool> SkuJaExisteAsync(string sku, Guid? ignorarProductId = null, CancellationToken cancellationToken = default);

    Task<bool> CodigoDeBarrasJaExisteAsync(string barcode, Guid? ignorarProductId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> PesquisarAsync(string? nome, string? sku, Guid? categoryId, bool? isActive,
        int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> ListarComEstoqueBaixoAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> ListarSemEstoqueAsync(CancellationToken cancellationToken = default);
}
