using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdegaDoRatao.API.Controllers;

[ApiController, Route("api/suppliers"), Authorize]
public sealed class SuppliersController(ISupplierService service) : ControllerBase
{
    [HttpGet, Authorize(Policy="purchases.read")] public Task<IReadOnlyList<SupplierResponse>> List(CancellationToken ct)=>service.ListAsync(ct);
    [HttpGet("{id:guid}"), Authorize(Policy="purchases.read")] public Task<SupplierResponse> Get(Guid id,CancellationToken ct)=>service.GetByIdAsync(id,ct);
    [HttpPost, Authorize(Policy="purchases.write")] public async Task<ActionResult<SupplierResponse>> Create(CreateSupplierRequest request,CancellationToken ct){var item=await service.CreateAsync(request,ct);return CreatedAtAction(nameof(Get),new{id=item.Id},item);}
    [HttpPut("{id:guid}"), Authorize(Policy="purchases.write")] public Task<SupplierResponse> Update(Guid id,UpdateSupplierRequest request,CancellationToken ct)=>service.UpdateAsync(id,request,ct);
    [HttpPatch("{id:guid}/active"), Authorize(Policy="purchases.write")] public async Task<IActionResult> Active(Guid id,ActiveStatusRequest request,CancellationToken ct){await service.SetActiveAsync(id,request.IsActive,ct);return NoContent();}
}
