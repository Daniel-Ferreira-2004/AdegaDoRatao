using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Enums;
using AdegaDoRatao.Domain.Interfaces;
using AdegaDoRatao.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AdegaDoRatao.Infrastructure.Repositories;

/// <summary>
/// Persistência de vendas. Sempre carrega os itens no GET por id e na
/// listagem paginada (necessário para ItemCount e para o cancelamento).
/// </summary>
public sealed class SaleRepository(AppDbContext context) : Repository<Sale>(context), ISaleRepository
{
    public override Task<Sale?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
        => Set.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<long> ObterProximoNumeroDeVendaAsync(CancellationToken ct = default)
        => (await Set.MaxAsync(x => (long?)x.SaleNumber, ct) ?? 0) + 1;

    public async Task<IReadOnlyList<Sale>> PesquisarAsync(
        DateTime? from, DateTime? to, SaleStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Filtrar(from, to, status);
        return await query
            .Include(x => x.Items)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.SaleNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public Task<int> ContarAsync(DateTime? from, DateTime? to, SaleStatus? status, CancellationToken ct = default)
        => Filtrar(from, to, status).CountAsync(ct);

    private IQueryable<Sale> Filtrar(DateTime? from, DateTime? to, SaleStatus? status)
    {
        var query = Set.AsQueryable();
        if (from.HasValue) query = query.Where(x => x.Date >= from.Value);
        if (to.HasValue) query = query.Where(x => x.Date <= to.Value);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        return query;
    }

    // ========================================================================
    // Dashboard (RF21)
    // ========================================================================

    public Task<int> ContarPorPeriodoAsync(DateTime from, DateTime to, CancellationToken ct = default)
        => Set.CountAsync(x => x.Date >= from && x.Date <= to && x.Status == SaleStatus.Concluida, ct);

    public async Task<decimal> SomarTotalPorPeriodoAsync(DateTime from, DateTime to, CancellationToken ct = default)
        => await Set.Where(x => x.Date >= from && x.Date <= to && x.Status == SaleStatus.Concluida)
            .SumAsync(x => (decimal?)x.Total, ct) ?? 0;

    public async Task<IReadOnlyList<Sale>> ListarRecentesAsync(int count, CancellationToken ct = default)
        => await Set.Include(x => x.Items)
            .OrderByDescending(x => x.Date)
            .Take(count)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<(Guid ProductId, string ProductName, string Sku, int TotalQuantity, decimal TotalRevenue)>>
        ListarMaisVendidosAsync(DateTime from, DateTime to, int count, CancellationToken ct = default)
    {
        var result = await Context.SaleItems
            .Join(Context.Sales, si => si.SaleId, s => s.Id, (si, s) => new { si, s })
            .Where(x => x.s.Date >= from && x.s.Date <= to && x.s.Status == SaleStatus.Concluida)
            .Join(Context.Products, x => x.si.ProductId, p => p.Id, (x, p) => new { x.si, p })
            .GroupBy(x => new { x.p.Id, x.p.Name, x.p.Sku })
            .Select(g => new
            {
                g.Key.Id,
                g.Key.Name,
                g.Key.Sku,
                TotalQuantity = g.Sum(x => x.si.Quantity),
                TotalRevenue = g.Sum(x => x.si.Quantity * x.si.UnitPrice)
            })
            .OrderByDescending(x => x.TotalQuantity)
            .Take(count)
            .ToListAsync(ct);

        return result.Select(x => (x.Id, x.Name, x.Sku, x.TotalQuantity, x.TotalRevenue)).ToList();
    }

    public async Task<IReadOnlyList<(Guid PaymentMethodId, string PaymentMethodName, int TotalSales, decimal TotalAmount)>>
        ListarFormasPagamentoMaisUsadasAsync(DateTime from, DateTime to, int count, CancellationToken ct = default)
    {
        var result = await Set
            .Where(x => x.Date >= from && x.Date <= to && x.Status == SaleStatus.Concluida)
            .Join(Context.PaymentMethods, s => s.PaymentMethodId, pm => pm.Id, (s, pm) => new { s, pm })
            .GroupBy(x => new { x.pm.Id, x.pm.Name })
            .Select(g => new
            {
                g.Key.Id,
                g.Key.Name,
                TotalSales = g.Count(),
                TotalAmount = g.Sum(x => x.s.Total)
            })
            .OrderByDescending(x => x.TotalSales)
            .Take(count)
            .ToListAsync(ct);

        return result.Select(x => (x.Id, x.Name, x.TotalSales, x.TotalAmount)).ToList();
    }

    public async Task<decimal> SomarLucroBrutoEstimadoAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        // RN28: Lucro bruto = soma (preço venda - preço custo) × quantidade
        var result = await Context.SaleItems
            .Join(Context.Sales, si => si.SaleId, s => s.Id, (si, s) => new { si, s })
            .Where(x => x.s.Date >= from && x.s.Date <= to && x.s.Status == SaleStatus.Concluida)
            .Join(Context.Products, x => x.si.ProductId, p => p.Id, (x, p) => new
            {
                Lucro = (x.si.UnitPrice - p.CostPrice) * x.si.Quantity
            })
            .SumAsync(x => (decimal?)x.Lucro, ct);

        return result ?? 0;
    }
}
