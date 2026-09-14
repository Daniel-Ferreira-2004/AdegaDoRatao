namespace AdegaDoRatao.Application.DTOs;
public sealed record CreateSupplierRequest(string Name, string? Document, string? Phone, string? Email);
public sealed record UpdateSupplierRequest(string Name, string? Document, string? Phone, string? Email);
public sealed record SupplierResponse(Guid Id, string Name, string? Document, string? Phone, string? Email, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);
