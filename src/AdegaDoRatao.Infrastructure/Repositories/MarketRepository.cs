using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Interfaces;
using AdegaDoRatao.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AdegaDoRatao.Infrastructure.Repositories;

public sealed class MarketRepository : Repository<Market>, IMarketRepository
{
    public MarketRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Market>> ListarAtivosAsync(CancellationToken cancellationToken = default)
        => await Set.Where(m => m.IsActive).ToListAsync(cancellationToken);
}
