using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Enums;

namespace AdegaDoRatao.Domain.Interfaces;

public interface ISupplierRepository : IRepository<Supplier>
{
}

public interface IPurchaseRepository : IRepository<Purchase>
{
    /// <summary>
    /// Soma o estoque restante da mercadoria de um item específico, entre
    /// esta compra e todas as saídas já ocorridas, usada por RN17 para
    /// decidir se o cancelamento de uma compra confirmada é seguro.
    /// </summary>
    Task<int> QuantidadeAindaEmEstoqueDoItemAsync(Guid purchaseId, Guid productId, CancellationToken cancellationToken = default);
}

public interface ISaleRepository : IRepository<Sale>
{
    /// <summary>Próximo número sequencial do cupom/venda (RN18). Deve ser chamado dentro da transação da venda.</summary>
    Task<long> ObterProximoNumeroDeVendaAsync(CancellationToken cancellationToken = default);

    /// <summary>Lista paginada para consumo mobile: só a página pedida, mais recente primeiro.</summary>
    Task<IReadOnlyList<Sale>> PesquisarAsync(DateTime? from, DateTime? to, SaleStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<int> ContarAsync(DateTime? from, DateTime? to, SaleStatus? status, CancellationToken cancellationToken = default);

    // Dashboard (RF21)
    Task<int> ContarPorPeriodoAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<decimal> SomarTotalPorPeriodoAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sale>> ListarRecentesAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(Guid ProductId, string ProductName, string Sku, int TotalQuantity, decimal TotalRevenue)>> ListarMaisVendidosAsync(DateTime from, DateTime to, int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(Guid PaymentMethodId, string PaymentMethodName, int TotalSales, decimal TotalAmount)>> ListarFormasPagamentoMaisUsadasAsync(DateTime from, DateTime to, int count, CancellationToken cancellationToken = default);
    Task<decimal> SomarLucroBrutoEstimadoAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}

public interface IPaymentMethodRepository : IRepository<PaymentMethod>
{
    Task<bool> NomeJaExisteAsync(string name, Guid? ignoreId = null, CancellationToken cancellationToken = default);
}
