namespace AdegaDoRatao.Application.Common;

/// <summary>Resposta de uma operação sem conteúdo de retorno.</summary>
public sealed record Result(bool Succeeded, string? Error = null)
{
    public static Result Success() => new(true);
    public static Result Failure(string error) => new(false, error);
}

/// <summary>Resposta de uma operação que devolve um conteúdo tipado.</summary>
public sealed record Result<T>(bool Succeeded, T? Value = default, string? Error = null)
{
    public static Result<T> Success(T value) => new(true, value);
    public static Result<T> Failure(string error) => new(false, default, error);
}
