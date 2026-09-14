namespace AdegaDoRatao.Application.DTOs;

// ============================================================================
// Auditoria (RN32/RN33, UC11) — somente leitura; registros são imutáveis.
// ============================================================================

/// <summary>Registro de auditoria retornado pela API.</summary>
public sealed record AuditLogResponse(
    Guid Id,
    Guid? UserId,
    string? UserEmail,
    string Action,
    string EntityName,
    string EntityId,
    string? OldValues,
    string? NewValues,
    DateTime CreatedAt);

/// <summary>Filtro de listagem geral de auditoria (somente ADMIN).</summary>
public sealed record AuditLogQuery(
    string? EntityName,
    string? EntityId,
    Guid? UserId,
    string? Action,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 20);
