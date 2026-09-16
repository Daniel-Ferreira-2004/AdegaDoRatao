namespace AdegaDoRatao.Application.Interfaces;

/// <summary>Delimita operações que precisam ser gravadas atomicamente.</summary>
public interface IUnitOfWork
{
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>Expõe apenas a identidade do usuário autenticado ao caso de uso.</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public interface IJwtTokenGenerator
{
    /// <summary>Tempo de expiração do token em minutos (Jwt:ExpirationMinutes).</summary>
    int ExpirationMinutes { get; }
    string Generate(Guid userId, string email, string role, IEnumerable<string> permissions);
}
