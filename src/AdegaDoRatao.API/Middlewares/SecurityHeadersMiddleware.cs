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

        // O Swagger UI (exposto apenas em Development) usa scripts e estilos
        // inline, que o CSP restritivo bloquearia (tela branca). Para as
        // rotas /swagger usamos uma política que permite inline; todo o
        // resto da API continua com a política conservadora.
        headers["Content-Security-Policy"] =
            context.Request.Path.StartsWithSegments("/swagger")
                ? "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:"
                : "default-src 'self'";

        await next(context);
    }
}

/// <summary>Extensão para registrar o middleware no pipeline.</summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}
