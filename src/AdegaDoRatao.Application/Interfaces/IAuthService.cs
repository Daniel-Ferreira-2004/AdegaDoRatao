using AdegaDoRatao.Application.DTOs;
namespace AdegaDoRatao.Application.Interfaces;
public interface IAuthService { Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default); }
