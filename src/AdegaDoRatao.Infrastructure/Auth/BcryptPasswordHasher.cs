using AdegaDoRatao.Application.Interfaces;

namespace AdegaDoRatao.Infrastructure.Auth;

/// <summary>
/// Adaptador de BCrypt. A Application conhece somente IPasswordHasher e não
/// fica acoplada a uma biblioteca ou algoritmo específico.
/// </summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    // Custo 12 é um equilíbrio seguro para um sistema de porte pequeno; ele
    // pode ser elevado no futuro conforme a capacidade da infraestrutura.
    private const int WorkFactor = 12;

    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("A senha é obrigatória.", nameof(password));
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool Verify(string password, string passwordHash)
        => !string.IsNullOrWhiteSpace(password)
           && !string.IsNullOrWhiteSpace(passwordHash)
           && BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
