using System.Net;
using System.Text.Json;
using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace AdegaDoRatao.API.Middlewares;

/// <summary>
/// Tratamento global de erros (RNF06).
///
/// Converte exceções em respostas ProblemDetails (RFC 7807), padronizando
/// o formato de erro da API:
///
/// - <see cref="DomainException"/> → 400 (regra de negócio violada)
/// - <see cref="UseCaseException"/> → 400 (erro previsível de caso de uso)
/// - <see cref="UnauthorizedAccessException"/> → 403
/// - Qualquer outra → 500 (sem stack trace em produção)
///
/// O middleware também garante que todo erro seja logado com o
/// TraceId da requisição, permitindo correlacionar o erro que o
/// cliente recebeu com o log interno (RNF05).
/// </summary>
public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            DomainException => (HttpStatusCode.BadRequest, "Regra de negócio violada"),
            UseCaseException => (HttpStatusCode.BadRequest, "Operação não permitida"),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Acesso negado"),
            _ => (HttpStatusCode.InternalServerError, "Erro interno do servidor")
        };

        // Erros de negócio (4xx) são esperados — log como Warning.
        // Erros inesperados (5xx) indicam bug/falha — log como Error com exceção completa.
        if ((int)statusCode >= 500)
        {
            logger.LogError(exception, "Erro inesperado ao processar {Method} {Path}. TraceId: {TraceId}",
                context.Request.Method, context.Request.Path, context.TraceIdentifier);
        }
        else
        {
            logger.LogWarning("Erro de negócio ao processar {Method} {Path}: {Message}. TraceId: {TraceId}",
                context.Request.Method, context.Request.Path, exception.Message, context.TraceIdentifier);
        }

        // Em produção, erros 500 não expõem a mensagem real (pode conter
        // detalhes internos). Em desenvolvimento, expõe para facilitar debug.
        var detail = (int)statusCode >= 500 && !context.RequestServices
            .GetRequiredService<IHostEnvironment>().IsDevelopment()
            ? "Ocorreu um erro inesperado. Informe o TraceId ao suporte."
            : exception.Message;

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Extensions = { ["traceId"] = context.TraceIdentifier }
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}

/// <summary>Extensão para registrar o middleware no pipeline.</summary>
public static class ErrorHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ErrorHandlingMiddleware>();
}
