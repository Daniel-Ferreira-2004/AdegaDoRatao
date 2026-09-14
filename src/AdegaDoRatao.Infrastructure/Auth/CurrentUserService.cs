using AdegaDoRatao.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;

namespace AdegaDoRatao.Infrastructure.Auth;

/// <summary>
/// Lê a identidade da requisição atual. Services recebem esta abstração, em
/// vez de HttpContext, para continuarem independentes de HTTP e testáveis.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public CurrentUserService(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;
    private System.Security.Claims.ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
    public string? Email => User?.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? User?.Identity?.Name;
    public Guid? UserId => Guid.TryParse(User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
}
