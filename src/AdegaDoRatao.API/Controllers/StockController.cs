using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdegaDoRatao.API.Controllers;

[ApiController]
[Route("api/stock")]
[Authorize]
public sealed class StockController(IStockService stockService, IProductService productService) : ControllerBase
{
    [HttpPost("movements"), Authorize(Policy="stock.write")]
    public async Task<ActionResult<StockMovementResponse>> Register(RegisterStockMovementRequest request, CancellationToken ct)
        => Ok(await stockService.RegisterMovementAsync(request, ct));

    [HttpGet("products/{productId:guid}/movements"), Authorize(Policy="stock.read")]
    public Task<IReadOnlyList<StockMovementResponse>> History(Guid productId, DateTime? from, DateTime? to, CancellationToken ct)
        => stockService.GetHistoryAsync(productId, from, to, ct);

    [HttpGet("low"), Authorize(Policy="stock.read")]
    public Task<IReadOnlyList<ProductResponse>> LowStock([FromQuery] bool includeOutOfStock, CancellationToken ct)
        => productService.GetLowStockAsync(includeOutOfStock, ct);
}
