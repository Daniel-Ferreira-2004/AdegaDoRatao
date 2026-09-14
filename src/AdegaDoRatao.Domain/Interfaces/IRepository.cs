using AdegaDoRatao.Domain.Common;

namespace AdegaDoRatao.Domain.Interfaces;

/// <summary>
/// Contrato genérico de repositório, com as operações comuns a toda
/// entidade. Repositórios específicos (ex.: IProductRepository) herdam
/// desta interface e adicionam apenas os métodos de consulta especiais
/// daquela entidade (ex.: BuscarPorSkuAsync).
///
/// Esta interface é definida no Domínio, mas implementada na Infrastructure
/// (com Entity Framework Core) — é o "Dependency Inversion Principle" (o D
/// do SOLID) em ação: a Application depende desta abstração, não do EF Core.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> ListarTodosAsync(CancellationToken cancellationToken = default);

    Task AdicionarAsync(T entidade, CancellationToken cancellationToken = default);

    void Atualizar(T entidade);
}
