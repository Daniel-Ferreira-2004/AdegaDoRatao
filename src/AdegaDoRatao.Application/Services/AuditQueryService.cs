using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Interfaces;

namespace AdegaDoRatao.Application.Services;

/// <summary>
/// Consulta da trilha de auditoria (UC11). Somente leitura — RN33 garante
/// que os registros são imutáveis, então este serviço não expõe nenhuma
/// operação de escrita.
/// </summary>
public sealed class AuditQueryService(
    IAuditLogRepository auditLogs,
    IUserRepository users) : IAuditQueryService
{
    public async Task<IReadOnlyList<AuditLogResponse>> GetEntityTrailAsync(
        string entityName, string entityId, CancellationToken cancellationToken = default)
    {
        var logs = await auditLogs.ListarPorEntidadeAsync(entityName, entityId, cancellationToken);
        return await MapAsync(logs, cancellationToken);
    }

    public async Task<PagedResult<AuditLogResponse>> SearchAsync(
        AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var items = await auditLogs.PesquisarAsync(
            query.EntityName, query.EntityId, query.UserId, query.Action,
            query.From, query.To, page, pageSize, cancellationToken);
        var total = await auditLogs.ContarAsync(
            query.EntityName, query.EntityId, query.UserId, query.Action,
            query.From, query.To, cancellationToken);

        var mapped = await MapAsync(items, cancellationToken);
        return new PagedResult<AuditLogResponse>(mapped, page, pageSize, total);
    }

    /// <summary>Resolve o e-mail dos usuários que aparecem nos registros.</summary>
    private async Task<IReadOnlyList<AuditLogResponse>> MapAsync(
        IReadOnlyList<AuditLog> logs, CancellationToken cancellationToken)
    {
        var userIds = logs.Where(x => x.UserId.HasValue).Select(x => x.UserId!.Value).Distinct().ToArray();
        var emails = new Dictionary<Guid, string?>();
        foreach (var userId in userIds)
        {
            var user = await users.ObterPorIdAsync(userId, cancellationToken);
            emails[userId] = user?.Email;
        }

        return logs.Select(x => new AuditLogResponse(
            x.Id, x.UserId, x.UserId.HasValue ? emails.GetValueOrDefault(x.UserId.Value) : null,
            x.Action, x.EntityName, x.EntityId, x.OldValues, x.NewValues, x.CreatedAt)).ToArray();
    }
}
