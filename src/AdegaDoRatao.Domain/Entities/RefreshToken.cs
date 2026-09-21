using AdegaDoRatao.Domain.Common;

namespace AdegaDoRatao.Domain.Entities;

/// <summary>
/// Refresh token: credencial de longa duração (ex.: 30 dias) que permite
/// obter um novo access token JWT sem pedir senha novamente.
///
/// Segurança:
/// - Apenas o HASH (SHA-256) do token é persistido — se o banco vazar,
///   os tokens não podem ser reutilizados;
/// - Rotação: a cada uso, o token antigo é revogado e um novo é emitido.
///   Se um token revogado for reapresentado, indica roubo/replay.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    private RefreshToken()
    {
    }

    public RefreshToken(Guid userId, string tokenHash, DateTime expiresAt)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("O usuário é obrigatório.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("O hash do token é obrigatório.", nameof(tokenHash));
        }

        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    /// <summary>Token utilizável: não revogado e dentro da validade.</summary>
    public bool EstaValido => RevokedAt is null && ExpiresAt > DateTime.UtcNow;

    public void Revogar()
    {
        if (RevokedAt is null)
        {
            RevokedAt = DateTime.UtcNow;
            MarcarComoAtualizada();
        }
    }
}
