using System.Security.Cryptography;
using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Interfaces;

namespace AdegaDoRatao.Application.Services;
/// <summary>Autentica sem expor se o e-mail ou a senha causou a falha.</summary>
public sealed class AuthService(IUserRepository users, IRoleRepository roles, IPasswordHasher hasher, IJwtTokenGenerator tokens, IAuditService audit, IRefreshTokenRepository refreshTokens, IUnitOfWork unitOfWork) : IAuthService
{
    /// <summary>Validade do refresh token (o JWT continua com a expiração curta de JwtOptions).</summary>
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await users.ObterPorEmailAsync(request.Email, ct);
        if (user is null || !user.IsActive || !hasher.Verify(request.Password, user.PasswordHash))
        {
            // Auditoria de tentativa falha (hardening). NUNCA registrar a senha
            // (nem em texto nem em hash) em OldValues/NewValues — apenas o e-mail.
            await audit.RegisterAsync("LOGIN_FAILED", "User", request.Email, null, null, ct);
            throw new UseCaseException("E-mail ou senha inválidos.");
        }
        return await EmitirTokensAsync(user, ct);
    }

    public async Task<LoginResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var stored = await refreshTokens.ObterPorTokenHashAsync(Hash(request.RefreshToken), ct);
        if (stored is null || !stored.EstaValido)
        {
            // Token inexistente, expirado ou já revogado (possível replay de
            // token roubado) — mesma mensagem genérica, sem dar pistas.
            throw new UseCaseException("Sessão expirada. Faça login novamente.");
        }

        var user = await users.ObterPorIdAsync(stored.UserId, ct);
        if (user is null || !user.IsActive)
        {
            throw new UseCaseException("Sessão expirada. Faça login novamente.");
        }

        // Rotação: o token usado é revogado e substituído por um novo.
        stored.Revogar();
        refreshTokens.Atualizar(stored);
        return await EmitirTokensAsync(user, ct);
    }

    public async Task RevokeAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var stored = await refreshTokens.ObterPorTokenHashAsync(Hash(request.RefreshToken), ct);
        if (stored is null) return; // idempotente: revogar token desconhecido não é erro
        stored.Revogar();
        refreshTokens.Atualizar(stored);
        await unitOfWork.SaveChangesAsync(ct);
    }

    /// <summary>Gera o par JWT + refresh token, persistindo apenas o hash do refresh.</summary>
    private async Task<LoginResponse> EmitirTokensAsync(Domain.Entities.User user, CancellationToken ct)
    {
        var role = await roles.ObterPorIdAsync(user.RoleId, ct) ?? throw new UseCaseException("Perfil do usuário não encontrado.");
        var permissions = await roles.ObterPermissoesAsync(role.Id, ct);
        var token = tokens.Generate(user.Id, user.Email, role.Name, permissions);

        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        await refreshTokens.AdicionarAsync(new RefreshToken(user.Id, Hash(refreshToken), DateTime.UtcNow.Add(RefreshTokenLifetime)), ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new LoginResponse(token, DateTime.UtcNow.AddMinutes(tokens.ExpirationMinutes), refreshToken, user.Id, user.Name, role.Name, permissions);
    }

    /// <summary>Apenas o hash SHA-256 do refresh token é persistido (nunca o valor em claro).</summary>
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
}
