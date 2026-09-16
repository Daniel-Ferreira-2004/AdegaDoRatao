namespace AdegaDoRatao.API.Middlewares;

/// <summary>
/// Cabeçalhos de segurança HTTP (hardening).
///
/// Adiciona em toda resposta:
/// - X-Content-Type-Options: nosniff (bloqueia MIME sniffing)
/// - X-Frame-Options: DENY (bloqueia clickjacking)
/// - Referrer-Policy: strict-origin-when-cross-origin
/// - Content-Security-Policy: default-src 'self' (política conservadora;
///   a API não serve conteúdo HTML próprio além do Swagger em Development)
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Content-Security-Policy"] = "default-src 'self'";
        await next(context);
    }
}

/// <summary>Extensão para registrar o middleware no pipeline.</summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}
