using AdegaDoRatao.Application.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace AdegaDoRatao.Infrastructure.Persistence;

/// <summary>Implementa a fronteira transacional usada pelos serviços de venda e compra.</summary>
public sealed class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;
    public UnitOfWork(AppDbContext context) => _context = context;
    public async Task BeginTransactionAsync(CancellationToken ct = default) => _transaction ??= await _context.Database.BeginTransactionAsync(ct);
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;
        await _transaction.CommitAsync(ct); await _transaction.DisposeAsync(); _transaction = null;
    }
    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;
        await _transaction.RollbackAsync(ct); await _transaction.DisposeAsync(); _transaction = null;
    }
    public void Dispose() => _transaction?.Dispose();
}
