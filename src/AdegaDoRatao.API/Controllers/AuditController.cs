using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdegaDoRatao.API.Controllers;

/// <summary>
/// Consulta da trilha de auditoria (UC11). Somente leitura — os registros
/// são imutáveis (RN33). Restrito a ADMIN (RN31).
/// </summary>
[ApiController]
[Route("api/audit")]
[Authorize(Policy = "audit.read")]
public sealed class AuditController(IAuditQueryService service) : ControllerBase
{
    /// <summary>Trilha de auditoria de uma entidade específica (ex.: GET /api/audit/Product/{id}).</summary>
    [HttpGet("{entityName}/{entityId}")]
    public Task<IReadOnlyList<AuditLogResponse>> GetEntityTrail(
        string entityName, string entityId, CancellationToken ct)
        => service.GetEntityTrailAsync(entityName, entityId, ct);

    /// <summary>Listagem geral com filtros (entidade, usuário, ação, período) e paginação.</summary>
    [HttpGet]
    public Task<PagedResult<AuditLogResponse>> Search([FromQuery] AuditLogQuery query, CancellationToken ct)
        => service.SearchAsync(query, ct);
}
