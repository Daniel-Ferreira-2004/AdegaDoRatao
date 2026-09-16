using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AdegaDoRatao.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Emite um token para um usuário ativo, sem expor dados sensíveis.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("login")] // máx. 5 tentativas/min por IP (anti força bruta)
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
        => Ok(await authService.LoginAsync(request, cancellationToken));
}
