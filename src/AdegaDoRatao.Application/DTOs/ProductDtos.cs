namespace AdegaDoRatao.Application.DTOs;

public sealed record CreateProductRequest(string Name, string? Description, string Barcode,
    Guid CategoryId, Guid BrandId, string UnitOfMeasure, decimal CostPrice, decimal SalePrice,
    int MinStock, int? MaxStock, bool AllowNegativeStock = false);

public sealed record UpdateProductRequest(string Name, string? Description, string Barcode, Guid CategoryId, Guid BrandId,
    string UnitOfMeasure, int MinStock, int? MaxStock, bool AllowNegativeStock);

public sealed record ChangeProductPricesRequest(decimal CostPrice, decimal SalePrice);

public sealed record ProductQuery(string? Name, string? Sku, Guid? CategoryId, bool? IsActive,
    int Page = 1, int PageSize = 20);

public sealed record ProductResponse(Guid Id, string Name, string? Description, string Sku, string? Barcode,
    Guid CategoryId, Guid BrandId, string UnitOfMeasure, decimal CostPrice, decimal SalePrice,
    int CurrentStock, int MinStock, int? MaxStock, bool AllowNegativeStock, bool IsActive,
    DateTime CreatedAt, DateTime? UpdatedAt);
