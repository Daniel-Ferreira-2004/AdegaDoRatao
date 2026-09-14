using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;

namespace AdegaDoRatao.Application.Interfaces;

public interface IProductService
{
    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<ProductResponse>> SearchAsync(ProductQuery query, CancellationToken cancellationToken = default);
    Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse> ChangePricesAsync(Guid id, ChangeProductPricesRequest request, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductResponse>> GetLowStockAsync(bool includeOutOfStock, CancellationToken cancellationToken = default);
}

public interface IStockService
{
    Task<StockMovementResponse> RegisterMovementAsync(RegisterStockMovementRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockMovementResponse>> GetHistoryAsync(Guid productId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
}

public interface ICategoryService
{
    Task<NamedEntityResponse> CreateAsync(NamedEntityRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NamedEntityResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default);
}

public interface IBrandService
{
    Task<NamedEntityResponse> CreateAsync(NamedEntityRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NamedEntityResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default);
}

public interface ISaleService
{
    Task<SaleResponse> CreateAsync(CreateSaleRequest request, CancellationToken cancellationToken = default);
    Task<SaleResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<SaleListItemResponse>> SearchAsync(SaleQuery query, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IPaymentMethodService
{
    Task<NamedEntityResponse> CreateAsync(NamedEntityRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NamedEntityResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default);
}

public interface IPurchaseService
{
    Task<PurchaseResponse> CreateAsync(CreatePurchaseRequest request, CancellationToken cancellationToken = default);
    Task ConfirmAsync(Guid id, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ISupplierService
{
    Task<SupplierResponse> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<SupplierResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupplierResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<SupplierResponse> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default);
}

/// <summary>
/// Casos de uso do módulo financeiro (RN24–RN27): categorias, lançamentos
/// manuais e fluxo de caixa. Lançamentos automáticos de venda/compra são
/// gerados pelos próprios SaleService/PurchaseService, não por aqui.
/// </summary>
public interface IFinancialService
{
    // Categorias financeiras
    Task<FinancialCategoryResponse> CreateCategoryAsync(CreateFinancialCategoryRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinancialCategoryResponse>> ListCategoriesAsync(CancellationToken cancellationToken = default);
    Task SetCategoryActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default);

    // Lançamentos manuais
    Task<FinancialTransactionResponse> CreateTransactionAsync(CreateFinancialTransactionRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<FinancialTransactionResponse>> SearchTransactionsAsync(FinancialTransactionQuery query, CancellationToken cancellationToken = default);
    Task EstornarAsync(Guid id, CancellationToken cancellationToken = default);

    // Fluxo de caixa
    Task<CashFlowResponse> GetCashFlowAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}

/// <summary>
/// Dashboard operacional (RF21): agrega vendas, financeiro e estoque em
/// uma única chamada — pensado para apps mobile, evitando múltiplas
/// requisições em rede móvel.
/// </summary>
public interface IDashboardService
{
    Task<DashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Consulta da trilha de auditoria (UC11). Somente leitura — RN33 garante
/// que os registros são imutáveis.
/// </summary>
public interface IAuditQueryService
{
    Task<IReadOnlyList<AuditLogResponse>> GetEntityTrailAsync(string entityName, string entityId, CancellationToken cancellationToken = default);
    Task<PagedResult<AuditLogResponse>> SearchAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
}
