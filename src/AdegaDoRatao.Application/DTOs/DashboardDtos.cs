namespace AdegaDoRatao.Application.DTOs;

// ============================================================================
// Dashboard (RF21, RN27–RN29)
// ============================================================================

/// <summary>Resumo geral do dashboard operacional.</summary>
public sealed record DashboardSummaryResponse(
    // Vendas
    int VendasHoje,
    int VendasMes,
    decimal FaturamentoHoje,
    decimal FaturamentoMes,

    // Financeiro
    decimal DespesasMes,
    decimal SaldoMes,

    // Estoque
    int ProdutosEstoqueBaixo,
    int ProdutosSemEstoque,

    // Lucro bruto estimado (RN28)
    decimal LucroBrutoEstimadoMes);

/// <summary>Venda recente para listagem no dashboard.</summary>
public sealed record RecentSaleResponse(
    Guid Id,
    long SaleNumber,
    DateTime Date,
    decimal Total,
    string PaymentMethodName,
    int ItemCount);

/// <summary>Movimentação de estoque recente para o dashboard.</summary>
public sealed record RecentStockMovementResponse(
    Guid Id,
    string ProductName,
    string Type,
    int Quantity,
    DateTime Date,
    string? Reason);

/// <summary>Produto mais vendido no período.</summary>
public sealed record TopSellingProductResponse(
    Guid ProductId,
    string ProductName,
    string Sku,
    int TotalQuantitySold,
    decimal TotalRevenue);

/// <summary>Forma de pagamento mais utilizada no período.</summary>
public sealed record TopPaymentMethodResponse(
    Guid PaymentMethodId,
    string PaymentMethodName,
    int TotalSales,
    decimal TotalAmount);

/// <summary>Resposta completa do dashboard.</summary>
public sealed record DashboardResponse(
    DashboardSummaryResponse Summary,
    IReadOnlyList<RecentSaleResponse> RecentSales,
    IReadOnlyList<RecentStockMovementResponse> RecentMovements,
    IReadOnlyList<TopSellingProductResponse> TopProducts,
    IReadOnlyList<TopPaymentMethodResponse> TopPaymentMethods);
