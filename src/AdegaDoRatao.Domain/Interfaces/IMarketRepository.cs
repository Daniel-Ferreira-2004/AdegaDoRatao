using AdegaDoRatao.Domain.Entities;

namespace AdegaDoRatao.Domain.Interfaces;

public interface IMarketRepository : IRepository<Market>
{
    /// <summary>
    /// Lista apenas os mercados ativos (IsActive = true), usados pelo
    /// filtro de "mercados próximos". O cálculo de distância é feito em
    /// memória (Haversine em C#) — ver docs/18-MERCADOS-PROXIMOS.md.
    /// </summary>
    Task<IReadOnlyList<Market>> ListarAtivosAsync(CancellationToken cancellationToken = default);
}
