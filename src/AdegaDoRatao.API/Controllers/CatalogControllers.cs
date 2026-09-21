using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdegaDoRatao.API.Controllers;

[ApiController, Authorize]
public sealed class CategoriesController(ICategoryService service) : ControllerBase
{
    [HttpGet("api/v1/categories"), Authorize(Policy="products.read")] public Task<IReadOnlyList<NamedEntityResponse>> List(CancellationToken ct)=>service.ListAsync(ct);
    [HttpPost("api/v1/categories"), Authorize(Policy="products.write")] public async Task<ActionResult<NamedEntityResponse>> Create(NamedEntityRequest request,CancellationToken ct){var item=await service.CreateAsync(request,ct);return Created($"api/v1/categories/{item.Id}",item);}
    [HttpPatch("api/v1/categories/{id:guid}/active"), Authorize(Policy="products.write")] public async Task<IActionResult> Active(Guid id,ActiveStatusRequest request,CancellationToken ct){await service.SetActiveAsync(id,request.IsActive,ct);return NoContent();}
}

[ApiController, Authorize]
public sealed class BrandsController(IBrandService service) : ControllerBase
{
    [HttpGet("api/v1/brands"), Authorize(Policy="products.read")] public Task<IReadOnlyList<NamedEntityResponse>> List(CancellationToken ct)=>service.ListAsync(ct);
    [HttpPost("api/v1/brands"), Authorize(Policy="products.write")] public async Task<ActionResult<NamedEntityResponse>> Create(NamedEntityRequest request,CancellationToken ct){var item=await service.CreateAsync(request,ct);return Created($"api/v1/brands/{item.Id}",item);}
    [HttpPatch("api/v1/brands/{id:guid}/active"), Authorize(Policy="products.write")] public async Task<IActionResult> Active(Guid id,ActiveStatusRequest request,CancellationToken ct){await service.SetActiveAsync(id,request.IsActive,ct);return NoContent();}
}
