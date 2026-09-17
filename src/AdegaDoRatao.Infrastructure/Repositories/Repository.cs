using AdegaDoRatao.Domain.Common;
using AdegaDoRatao.Domain.Interfaces;
using AdegaDoRatao.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AdegaDoRatao.Infrastructure.Repositories;

/// <summary>Base EF para operações comuns; repositórios específicos só guardam suas consultas próprias.</summary>
public abstract class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<T> Set;
    protected Repository(AppDbContext context) => (Context, Set) = (context, context.Set<T>());
    public virtual Task<T?> ObterPorIdAsync(Guid id, CancellationToken ct = default) => Set.FirstOrDefaultAsync(x => x.Id == id, ct);
    public virtual async Task<IReadOnlyList<T>> ListarTodosAsync(CancellationToken ct = default) => await Set.ToListAsync(ct);
    public Task AdicionarAsync(T entity, CancellationToken ct = default) => Set.AddAsync(entity, ct).AsTask();
    public void Atualizar(T entity) => Set.Update(entity);
    public void Remover(T entity) => Set.Remove(entity);
}
