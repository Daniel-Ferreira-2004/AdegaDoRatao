namespace AdegaDoRatao.Infrastructure.Auth;

/// <summary>
/// Configurações de autenticação lidas da seção <c>Jwt</c>. Segredos nunca
/// devem estar em código ou no appsettings versionado: use User Secrets no
/// desenvolvimento e variáveis de ambiente em produção.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Secret { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int ExpirationMinutes { get; init; } = 60;
}
