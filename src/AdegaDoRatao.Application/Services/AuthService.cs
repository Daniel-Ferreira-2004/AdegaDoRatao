using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Interfaces;

namespace AdegaDoRatao.Application.Services;
/// <summary>Autentica sem expor se o e-mail ou a senha causou a falha.</summary>
public sealed class AuthService(IUserRepository users, IRoleRepository roles, IPasswordHasher hasher, IJwtTokenGenerator tokens, IAuditService audit) : IAuthService
{
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
        var role = await roles.ObterPorIdAsync(user.RoleId, ct) ?? throw new UseCaseException("Perfil do usuário não encontrado.");
        var permissions = await roles.ObterPermissoesAsync(role.Id, ct);
        var token = tokens.Generate(user.Id, user.Email, role.Name, permissions);
        return new LoginResponse(token, DateTime.UtcNow.AddMinutes(tokens.ExpirationMinutes), user.Id, user.Name, role.Name, permissions);
    }
}
