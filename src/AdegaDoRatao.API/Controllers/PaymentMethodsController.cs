using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdegaDoRatao.API.Controllers;

/// <summary>Cadastro mínimo de formas de pagamento, necessário para fechar uma venda.</summary>
[ApiController]
[Route("api/v1/payment-methods")]
[Authorize]
public sealed class PaymentMethodsController(IPaymentMethodService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "sales.read")]
    public Task<IReadOnlyList<NamedEntityResponse>> List(CancellationToken ct) => service.ListAsync(ct);

    [HttpPost]
    [Authorize(Policy = "sales.write")]
    public async Task<ActionResult<NamedEntityResponse>> Create(NamedEntityRequest request, CancellationToken ct)
    {
        var item = await service.CreateAsync(request, ct);
        return Created($"api/v1/payment-methods/{item.Id}", item);
    }

    [HttpPatch("{id:guid}/active")]
    [Authorize(Policy = "sales.write")]
    public async Task<IActionResult> SetActive(Guid id, ActiveStatusRequest request, CancellationToken ct)
    {
        await service.SetActiveAsync(id, request.IsActive, ct);
        return NoContent();
    }
}
