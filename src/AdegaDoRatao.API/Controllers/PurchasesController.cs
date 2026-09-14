using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdegaDoRatao.API.Controllers;

[ApiController, Route("api/purchases"), Authorize]
public sealed class PurchasesController(IPurchaseService service) : ControllerBase
{
    [HttpPost, Authorize(Policy="purchases.write")] public async Task<ActionResult<PurchaseResponse>> Create(CreatePurchaseRequest request,CancellationToken ct){var item=await service.CreateAsync(request,ct);return Created($"api/purchases/{item.Id}",item);}
    [HttpPost("{id:guid}/confirm"), Authorize(Policy="purchases.write")] public async Task<IActionResult> Confirm(Guid id,CancellationToken ct){await service.ConfirmAsync(id,ct);return NoContent();}
    [HttpPost("{id:guid}/cancel"), Authorize(Policy="purchases.write")] public async Task<IActionResult> Cancel(Guid id,CancellationToken ct){await service.CancelAsync(id,ct);return NoContent();}
}
