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

    /// <summary>
    /// Indica se o produto possui qualquer histórico (movimentação de estoque,
    /// item de compra ou item de venda). Usado pela RN36 para decidir entre
    /// exclusão física (sem histórico) ou apenas desativação (com histórico).
    /// </summary>
    Task<bool> PossuiHistoricoAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>EANs (códigos de barras) dos produtos ativos que possuem EAN cadastrado.</summary>
    Task<IReadOnlyList<string>> ListarEansAtivosAsync(CancellationToken cancellationToken = default);

    /// <summary>Pares (EAN, Nome) dos produtos ativos com EAN — usado pelo agente de preços para buscar também pelo nome.</summary>
    Task<IReadOnlyList<(string Ean, string Nome)>> ListarAtivosComEanAsync(CancellationToken cancellationToken = default);
}
