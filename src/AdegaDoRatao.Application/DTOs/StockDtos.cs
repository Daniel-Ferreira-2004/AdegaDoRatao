using AdegaDoRatao.Domain.Enums;

namespace AdegaDoRatao.Application.DTOs;

public sealed record RegisterStockMovementRequest(Guid ProductId, StockMovementType Type, int Quantity, string Reason);
public sealed record StockMovementResponse(Guid Id, Guid ProductId, StockMovementType Type, int Quantity,
    int PreviousStock, int NewStock, string Reason, Guid UserId, DateTime CreatedAt,
    string? ReferenceType, Guid? ReferenceId);
