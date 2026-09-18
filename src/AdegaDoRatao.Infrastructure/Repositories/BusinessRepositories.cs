using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Enums;
using AdegaDoRatao.Domain.Interfaces;
using AdegaDoRatao.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AdegaDoRatao.Infrastructure.Repositories;

public sealed class ProductRepository(AppDbContext c) : Repository<Product>(c), IProductRepository
{ public Task<Product?> ObterPorSkuAsync(string s,CancellationToken ct=default)=>Set.FirstOrDefaultAsync(x=>x.Sku==s.ToUpper(),ct); public Task<Product?> ObterPorCodigoDeBarrasAsync(string s,CancellationToken ct=default)=>Set.FirstOrDefaultAsync(x=>x.Barcode==s,ct); public Task<bool> SkuJaExisteAsync(string s,Guid? i=null,CancellationToken ct=default)=>Set.AnyAsync(x=>x.Sku==s.ToUpper()&&(!i.HasValue||x.Id!=i),ct); public Task<bool> CodigoDeBarrasJaExisteAsync(string s,Guid? i=null,CancellationToken ct=default)=>Set.AnyAsync(x=>x.Barcode==s&&(!i.HasValue||x.Id!=i),ct); public async Task<IReadOnlyList<Product>> PesquisarAsync(string? n,string? s,Guid? cat,bool? active,int p,int ps,CancellationToken ct=default){var q=Set.AsQueryable();if(!string.IsNullOrWhiteSpace(n))q=q.Where(x=>x.Name.Contains(n));if(!string.IsNullOrWhiteSpace(s))q=q.Where(x=>x.Sku.Contains(s));if(cat.HasValue)q=q.Where(x=>x.CategoryId==cat);if(active.HasValue)q=q.Where(x=>x.IsActive==active);return await q.OrderBy(x=>x.Name).Skip((p-1)*ps).Take(ps).ToListAsync(ct);} public async Task<IReadOnlyList<Product>> ListarComEstoqueBaixoAsync(CancellationToken ct=default)=>await Set.Where(x=>x.IsActive&&x.CurrentStock>0&&x.CurrentStock<=x.MinStock).ToListAsync(ct); public async Task<IReadOnlyList<Product>> ListarSemEstoqueAsync(CancellationToken ct=default)=>await Set.Where(x=>x.IsActive&&x.CurrentStock==0).ToListAsync(ct); public async Task<bool> PossuiHistoricoAsync(Guid id,CancellationToken ct=default)=>await Context.StockMovements.AnyAsync(x=>x.ProductId==id,ct)||await Context.PurchaseItems.AnyAsync(x=>x.ProductId==id,ct)||await Context.SaleItems.AnyAsync(x=>x.ProductId==id,ct); public async Task<IReadOnlyList<string>> ListarEansAtivosAsync(CancellationToken ct=default)=>await Set.Where(x=>x.IsActive&&x.Barcode!=null&&x.Barcode!="").Select(x=>x.Barcode!).Distinct().ToListAsync(ct); }
public sealed class CategoryRepository(AppDbContext c) : Repository<Category>(c), ICategoryRepository { public Task<bool> NomeJaExisteAsync(string n,Guid? i=null,CancellationToken ct=default)=>Set.AnyAsync(x=>x.Name.ToLower()==n.Trim().ToLower()&&(!i.HasValue||x.Id!=i),ct); }
public sealed class BrandRepository(AppDbContext c) : Repository<Brand>(c), IBrandRepository { public Task<bool> NomeJaExisteAsync(string n,Guid? i=null,CancellationToken ct=default)=>Set.AnyAsync(x=>x.Name.ToLower()==n.Trim().ToLower()&&(!i.HasValue||x.Id!=i),ct); }
public sealed class StockMovementRepository(AppDbContext c) : Repository<StockMovement>(c), IStockMovementRepository { public async Task<IReadOnlyList<StockMovement>> ListarPorProdutoAsync(Guid id,DateTime? f,DateTime? t,CancellationToken ct=default){var q=Set.Where(x=>x.ProductId==id);if(f.HasValue)q=q.Where(x=>x.CreatedAt>=f);if(t.HasValue)q=q.Where(x=>x.CreatedAt<=t);return await q.OrderByDescending(x=>x.CreatedAt).ToListAsync(ct);} public async Task<IReadOnlyList<(StockMovement Movement,string ProductName)>> ListarRecentesComProdutoAsync(int count,CancellationToken ct=default){var r=await Set.OrderByDescending(x=>x.CreatedAt).Take(count).Join(Context.Products,m=>m.ProductId,p=>p.Id,(m,p)=>new{m,p.Name}).ToListAsync(ct);return r.Select(x=>(x.m,x.Name)).ToList();} }
public sealed class SupplierRepository(AppDbContext c) : Repository<Supplier>(c), ISupplierRepository { }
public sealed class PaymentMethodRepository(AppDbContext c) : Repository<PaymentMethod>(c), IPaymentMethodRepository { public Task<bool> NomeJaExisteAsync(string n,Guid? i=null,CancellationToken ct=default)=>Set.AnyAsync(x=>x.Name.ToLower()==n.Trim().ToLower()&&(!i.HasValue||x.Id!=i),ct); }
public sealed class PurchaseRepository(AppDbContext c) : Repository<Purchase>(c), IPurchaseRepository { public override Task<Purchase?> ObterPorIdAsync(Guid id,CancellationToken ct=default)=>Set.Include(x=>x.Items).FirstOrDefaultAsync(x=>x.Id==id,ct); public async Task<int> QuantidadeAindaEmEstoqueDoItemAsync(Guid purchaseId,Guid productId,CancellationToken ct=default)=>await Context.PurchaseItems.Where(x=>x.PurchaseId==purchaseId&&x.ProductId==productId).SumAsync(x=>(int?)x.Quantity,ct)??0; }
public sealed class FinancialCategoryRepository(AppDbContext c) : Repository<FinancialCategory>(c), IFinancialCategoryRepository { }
public sealed class FinancialTransactionRepository(AppDbContext c) : Repository<FinancialTransaction>(c), IFinancialTransactionRepository { public async Task<IReadOnlyList<FinancialTransaction>> ListarPorPeriodoAsync(DateTime f,DateTime t,FinancialTransactionType? type,CancellationToken ct=default){var q=Set.Where(x=>x.Date>=f&&x.Date<=t);if(type.HasValue)q=q.Where(x=>x.Type==type);return await q.OrderByDescending(x=>x.Date).ToListAsync(ct);} public async Task<decimal> SomarPorTipoEPeriodoAsync(FinancialTransactionType type,DateTime f,DateTime t,CancellationToken ct=default)=>await Set.Where(x=>x.Type==type&&x.Date>=f&&x.Date<=t&&x.Status==FinancialTransactionStatus.Pago).SumAsync(x=>(decimal?)x.Amount,ct)??0; }
public sealed class UserRepository(AppDbContext c) : Repository<User>(c), IUserRepository { public Task<User?> ObterPorEmailAsync(string e,CancellationToken ct=default)=>Set.FirstOrDefaultAsync(x=>x.Email==e.Trim().ToLower(),ct); public Task<bool> EmailJaExisteAsync(string e,Guid? i=null,CancellationToken ct=default)=>Set.AnyAsync(x=>x.Email==e.Trim().ToLower()&&(!i.HasValue||x.Id!=i),ct); }
public sealed class RoleRepository(AppDbContext c) : Repository<Role>(c), IRoleRepository { public Task<Role?> ObterPorNomeAsync(string n,CancellationToken ct=default)=>Set.FirstOrDefaultAsync(x=>x.Name==n.Trim().ToUpper(),ct); public async Task<IReadOnlyCollection<string>> ObterPermissoesAsync(Guid roleId,CancellationToken ct=default)=>await Context.RolePermissions.Where(x=>x.RoleId==roleId).Join(Context.Permissions,rp=>rp.PermissionId,p=>p.Id,(rp,p)=>p.Code).ToListAsync(ct); }
public sealed class PermissionRepository(AppDbContext c) : Repository<Permission>(c), IPermissionRepository { }
public sealed class AuditLogRepository(AppDbContext c) : Repository<AuditLog>(c), IAuditLogRepository
{
    public async Task<IReadOnlyList<AuditLog>> ListarPorEntidadeAsync(string n, string id, CancellationToken ct = default)
        => await Set.Where(x => x.EntityName == n && x.EntityId == id).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<AuditLog>> PesquisarAsync(string? entityName, string? entityId, Guid? userId,
        string? action, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default)
        => await Filtrar(entityName, entityId, userId, action, from, to)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public Task<int> ContarAsync(string? entityName, string? entityId, Guid? userId,
        string? action, DateTime? from, DateTime? to, CancellationToken ct = default)
        => Filtrar(entityName, entityId, userId, action, from, to).CountAsync(ct);

    private IQueryable<AuditLog> Filtrar(string? entityName, string? entityId, Guid? userId,
        string? action, DateTime? from, DateTime? to)
    {
        var q = Set.AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityName)) q = q.Where(x => x.EntityName == entityName);
        if (!string.IsNullOrWhiteSpace(entityId)) q = q.Where(x => x.EntityId == entityId);
        if (userId.HasValue) q = q.Where(x => x.UserId == userId);
        if (!string.IsNullOrWhiteSpace(action)) q = q.Where(x => x.Action == action);
        if (from.HasValue) q = q.Where(x => x.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(x => x.CreatedAt <= to.Value);
        return q;
    }
}
