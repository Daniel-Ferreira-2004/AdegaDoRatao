using AdegaDoRatao.Domain.Entities;

namespace AdegaDoRatao.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> EmailJaExisteAsync(string email, Guid? ignorarUserId = null, CancellationToken cancellationToken = default);
}

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<string>> ObterPermissoesAsync(Guid roleId, CancellationToken cancellationToken = default);
}

public interface IPermissionRepository : IRepository<Permission>
{
}

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IReadOnlyList<AuditLog>> ListarPorEntidadeAsync(string entityName, string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>Pesquisa paginada com filtros (UC11). Registros são imutáveis (RN33) — somente leitura.</summary>
    Task<IReadOnlyList<AuditLog>> PesquisarAsync(string? entityName, string? entityId, Guid? userId,
        string? action, DateTime? from, DateTime? to, int page, int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> ContarAsync(string? entityName, string? entityId, Guid? userId,
        string? action, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
}
