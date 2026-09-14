namespace AdegaDoRatao.Application.Interfaces;

/// <summary>Contrato para registrar ações sensíveis sem acoplar serviços ao banco.</summary>
public interface IAuditService
{
    Task RegisterAsync(string action, string entityName, string entityId, object? oldValues,
        object? newValues, CancellationToken cancellationToken = default);
}
