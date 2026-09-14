using AdegaDoRatao.Domain.Enums;

namespace AdegaDoRatao.Application.DTOs;

public sealed record SaleItemRequest(Guid ProductId, int Quantity);
public sealed record CreateSaleRequest(IReadOnlyCollection<SaleItemRequest> Items, decimal Discount, Guid PaymentMethodId);
public sealed record SaleItemResponse(Guid ProductId, int Quantity, decimal UnitPrice, decimal Subtotal);

/// <summary>Detalhe da venda (itens inclusos). Usado em GET por id e no retorno do POST.</summary>
public sealed record SaleResponse(Guid Id, long SaleNumber, DateTime Date, decimal Discount, decimal Total,
    Guid PaymentMethodId, SaleStatus Status, IReadOnlyCollection<SaleItemResponse> Items);

/// <summary>Resumo enxuto para listagens mobile — sem itens, para reduzir tráfego.</summary>
public sealed record SaleListItemResponse(Guid Id, long SaleNumber, DateTime Date, decimal Discount, decimal Total,
    Guid PaymentMethodId, SaleStatus Status, int ItemCount);

public sealed record SaleQuery(DateTime? From, DateTime? To, SaleStatus? Status, int Page = 1, int PageSize = 20);

public sealed record PurchaseItemRequest(Guid ProductId, int Quantity, decimal UnitCost);
public sealed record CreatePurchaseRequest(Guid SupplierId, DateTime Date, decimal Discount, decimal Freight,
    Guid PaymentMethodId, IReadOnlyCollection<PurchaseItemRequest> Items);
public sealed record PurchaseResponse(Guid Id, Guid SupplierId, DateTime Date, decimal Discount, decimal Freight,
    decimal Total, PurchaseStatus Status);
