using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Interfaces;
using AdegaDoRatao.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AdegaDoRatao.Infrastructure.Repositories;

public sealed class MarketPriceSnapshotRepository(AppDbContext context)
    : Repository<MarketPriceSnapshot>(context), IMarketPriceSnapshotRepository
{
    public async Task<IReadOnlyList<MarketPriceSnapshot>> ListarPorEanAsync(
        string ean, CancellationToken cancellationToken = default)
        => await Set.Where(x => x.Ean == ean).ToListAsync(cancellationToken);

    public Task<MarketPriceSnapshot?> ObterPorEanERedeAsync(
        string ean, string rede, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(x => x.Ean == ean && x.Rede == rede, cancellationToken);

    public async Task<IReadOnlyList<string>> ListarEansDistintosAsync(CancellationToken cancellationToken = default)
        => await Set.Select(x => x.Ean).Distinct().ToListAsync(cancellationToken);
}
