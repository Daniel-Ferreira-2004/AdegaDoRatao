using AdegaDoRatao.Domain.Entities;

namespace AdegaDoRatao.Domain.Interfaces;

public interface IMarketPriceSnapshotRepository : IRepository<MarketPriceSnapshot>
{
    /// <summary>Snapshots mais recentes de um EAN (um por rede).</summary>
    Task<IReadOnlyList<MarketPriceSnapshot>> ListarPorEanAsync(string ean, CancellationToken cancellationToken = default);

    /// <summary>Obtém o snapshot de um EAN em uma rede específica.</summary>
    Task<MarketPriceSnapshot?> ObterPorEanERedeAsync(string ean, string rede, CancellationToken cancellationToken = default);

    /// <summary>EANs distintos já coletados (para o job saber o que atualizar).</summary>
    Task<IReadOnlyList<string>> ListarEansDistintosAsync(CancellationToken cancellationToken = default);
}
