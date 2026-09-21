using AdegaDoRatao.Application.DTOs;
namespace AdegaDoRatao.Application.Interfaces;
public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Troca um refresh token válido por um novo par (JWT + refresh token rotacionado).</summary>
    Task<LoginResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>Revoga um refresh token (logout / "sair de todos os dispositivos").</summary>
    Task RevokeAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
}
