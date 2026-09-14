namespace AdegaDoRatao.Domain.Exceptions;

/// <summary>
/// Exceção base para violações de regras de negócio do domínio.
///
/// Por que ter uma exceção própria (em vez de usar Exception genérica)?
/// Porque na Etapa 14 (tratamento global de erros) o middleware da API vai
/// capturar especificamente "DomainException" e convertê-la em um erro
/// HTTP 400/409 amigável (ProblemDetails), diferenciando de um erro
/// inesperado (500). Uma exceção genérica não permite essa diferenciação.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}
