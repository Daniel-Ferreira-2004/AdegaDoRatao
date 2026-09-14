using AdegaDoRatao.Application.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AdegaDoRatao.Infrastructure.Auth;

/// <summary>Cria tokens JWT assinados com as claims necessárias à autorização.</summary>
public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;
    public JwtTokenGenerator(IOptions<JwtOptions> options) => _options = options.Value;

    public string Generate(Guid userId, string email, string role, IEnumerable<string> permissions)
    {
        if (string.IsNullOrWhiteSpace(_options.Secret) || _options.Secret.Length < 32)
            throw new InvalidOperationException("Jwt:Secret deve ter pelo menos 32 caracteres e ser configurado fora do código-fonte.");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Role, role)
        };
        claims.AddRange(permissions.Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(permission => new Claim("permission", permission)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_options.Issuer, _options.Audience, claims,
            expires: DateTime.UtcNow.AddMinutes(Math.Max(1, _options.ExpirationMinutes)), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
