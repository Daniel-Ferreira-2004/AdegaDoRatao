using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdegaDoRatao.API.Controllers;

[ApiController, Authorize]
public sealed class CategoriesController(ICategoryService service) : ControllerBase
{
    [HttpGet("api/categories"), Authorize(Policy="products.read")] public Task<IReadOnlyList<NamedEntityResponse>> List(CancellationToken ct)=>service.ListAsync(ct);
    [HttpPost("api/categories"), Authorize(Policy="products.write")] public async Task<ActionResult<NamedEntityResponse>> Create(NamedEntityRequest request,CancellationToken ct){var item=await service.CreateAsync(request,ct);return Created($"api/categories/{item.Id}",item);}
    [HttpPatch("api/categories/{id:guid}/active"), Authorize(Policy="products.write")] public async Task<IActionResult> Active(Guid id,ActiveStatusRequest request,CancellationToken ct){await service.SetActiveAsync(id,request.IsActive,ct);return NoContent();}
}

[ApiController, Authorize]
public sealed class BrandsController(IBrandService service) : ControllerBase
{
    [HttpGet("api/brands"), Authorize(Policy="products.read")] public Task<IReadOnlyList<NamedEntityResponse>> List(CancellationToken ct)=>service.ListAsync(ct);
    [HttpPost("api/brands"), Authorize(Policy="products.write")] public async Task<ActionResult<NamedEntityResponse>> Create(NamedEntityRequest request,CancellationToken ct){var item=await service.CreateAsync(request,ct);return Created($"api/brands/{item.Id}",item);}
    [HttpPatch("api/brands/{id:guid}/active"), Authorize(Policy="products.write")] public async Task<IActionResult> Active(Guid id,ActiveStatusRequest request,CancellationToken ct){await service.SetActiveAsync(id,request.IsActive,ct);return NoContent();}
}
