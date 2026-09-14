using AdegaDoRatao.Domain.Entities;

namespace AdegaDoRatao.Domain.Interfaces;

public interface IStockMovementRepository : IRepository<StockMovement>
{
    Task<IReadOnlyList<StockMovement>> ListarPorProdutoAsync(Guid productId, DateTime? from, DateTime? to,
        CancellationToken cancellationToken = default);

    /// <summary>Lista as movimentações mais recentes com o nome do produto (para o dashboard).</summary>
    Task<IReadOnlyList<(StockMovement Movement, string ProductName)>> ListarRecentesComProdutoAsync(int count,
        CancellationToken cancellationToken = default);
}
