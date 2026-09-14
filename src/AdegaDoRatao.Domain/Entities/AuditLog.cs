using AdegaDoRatao.Domain.Common;

namespace AdegaDoRatao.Domain.Entities;

/// <summary>
/// Registro de auditoria (RN32/RN33) — imutável, nunca editado ou excluído.
/// OldValues/NewValues ficam como JSON serializado (a serialização em si é
/// feita na Application/Infrastructure; o domínio só guarda a string).
/// </summary>
public class AuditLog : BaseEntity
{
    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }

    private AuditLog()
    {
    }

    public AuditLog(Guid? userId, string action, string entityName, string entityId,
        string? oldValues, string? newValues)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("A ação auditada é obrigatória.", nameof(action));
        }

        if (string.IsNullOrWhiteSpace(entityName))
        {
            throw new ArgumentException("O nome da entidade auditada é obrigatório.", nameof(entityName));
        }

        UserId = userId;
        Action = action.Trim();
        EntityName = entityName.Trim();
        EntityId = entityId;
        OldValues = oldValues;
        NewValues = newValues;
    }
}
