using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdegaDoRatao.API.Controllers;

/// <summary>
/// Dashboard operacional (RF21): agrega vendas, financeiro, estoque e
/// indicadores em um único endpoint para o frontend.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController(IDashboardService service) : ControllerBase
{
    /// <summary>
    /// Retorna o dashboard completo: resumo, vendas recentes, movimentações,
    /// produtos mais vendidos e formas de pagamento mais usadas.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "dashboard.read")]
    public Task<DashboardResponse> Get(CancellationToken ct)
        => service.GetDashboardAsync(ct);
}
