namespace AdegaDoRatao.Application.Common;

/// <summary>
/// Erro previsível de caso de uso. A API o converterá para ProblemDetails,
/// sem revelar detalhes internos ao cliente.
/// O nome evita conflito com <see cref="System.ApplicationException"/>.
/// </summary>
public sealed class UseCaseException : Exception
{
    public UseCaseException(string message) : base(message) { }
}
