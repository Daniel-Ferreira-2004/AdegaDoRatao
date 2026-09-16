using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdegaDoRatao.API.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public sealed class ProductsController(IProductService service, IPrecoMercadoService precoMercado) : ControllerBase
{
    [HttpGet, Authorize(Policy = "products.read")]
    public Task<PagedResult<ProductResponse>> Search([FromQuery] ProductQuery query, CancellationToken ct) => service.SearchAsync(query, ct);
    [HttpGet("{id:guid}"), Authorize(Policy = "products.read")]
    public Task<ProductResponse> Get(Guid id, CancellationToken ct) => service.GetByIdAsync(id, ct);

    /// <summary>
    /// Compara o preço do produto nas redes de varejo da região (Veran,
    /// Shibata, Atacadão, Semar) via API externa Cnova Tech Data Market,
    /// usando o EAN cadastrado no produto. Resultado cacheado por 6h
    /// (configurável em DataMarket:CacheHours). Redes sem o produto vêm
    /// com disponivel=false — a chamada nunca retorna 500 por falha da
    /// API externa.
    /// </summary>
    [HttpGet("{id:guid}/precos-mercado"), Authorize(Policy = "products.read")]
    [ProducesResponseType(typeof(PrecoMercadoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<PrecoMercadoResponse> PrecosMercado(Guid id, CancellationToken ct) => precoMercado.ConsultarAsync(id, ct);
    [HttpPost, Authorize(Policy = "products.write")]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken ct) { var product = await service.CreateAsync(request, ct); return CreatedAtAction(nameof(Get), new { product.Id }, product); }
    [HttpPut("{id:guid}"), Authorize(Policy = "products.write")]
    public Task<ProductResponse> Update(Guid id, UpdateProductRequest request, CancellationToken ct) => service.UpdateAsync(id, request, ct);
    [HttpPatch("{id:guid}/prices"), Authorize(Policy = "products.write")]
    public Task<ProductResponse> ChangePrices(Guid id, ChangeProductPricesRequest request, CancellationToken ct) => service.ChangePricesAsync(id, request, ct);
    [HttpPatch("{id:guid}/active"), Authorize(Policy = "products.write")]
    public async Task<IActionResult> SetActive(Guid id, ActiveStatusRequest request, CancellationToken ct) { await service.SetActiveAsync(id, request.IsActive, ct); return NoContent(); }
}
