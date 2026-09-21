using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdegaDoRatao.API.Controllers;

/// <summary>
/// Endpoints de venda. O controller só traduz HTTP; a regra (estoque + caixa)
/// fica no SaleService. Operador precisa de sales.read/sales.write.
/// </summary>
[ApiController]
[Route("api/v1/sales")]
[Authorize]
public sealed class SalesController(ISaleService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "sales.read")]
    public Task<PagedResult<SaleListItemResponse>> Search([FromQuery] SaleQuery query, CancellationToken ct)
        => service.SearchAsync(query, ct);

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "sales.read")]
    public Task<SaleResponse> Get(Guid id, CancellationToken ct)
        => service.GetByIdAsync(id, ct);

    [HttpPost]
    [Authorize(Policy = "sales.write")]
    public async Task<ActionResult<SaleResponse>> Create(CreateSaleRequest request, CancellationToken ct)
    {
        var sale = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = sale.Id }, sale);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "sales.write")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await service.CancelAsync(id, ct);
        return NoContent();
    }
}
