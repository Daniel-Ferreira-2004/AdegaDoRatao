using AdegaDoRatao.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdegaDoRatao.Infrastructure.Persistence;

/// <summary>Contexto EF Core: a única porta da Infrastructure para o banco (PostgreSQL em produção; SQLite nos testes).</summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<FinancialCategory> FinancialCategories => Set<FinancialCategory>();
    public DbSet<FinancialTransaction> FinancialTransactions => Set<FinancialTransaction>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Market> Markets => Set<Market>();
    public DbSet<MarketPriceSnapshot> MarketPriceSnapshots => Set<MarketPriceSnapshot>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Ajustes para provedores que não suportam tipos/recursos específicos
        // do SQL Server (PostgreSQL em produção, SQLite nos testes de integração):
        // - nvarchar(max) vira TEXT
        // - rowversion (concorrência otimista) vira coluna comum
        if (Database.ProviderName != "Microsoft.EntityFrameworkCore.SqlServer")
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.GetColumnType() == "nvarchar(max)")
                    {
                        property.SetColumnType("TEXT");
                    }

                    if (property.IsConcurrencyToken && property.ClrType == typeof(byte[]))
                    {
                        property.IsConcurrencyToken = false;
                        property.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
                    }
                }
            }
        }

        base.OnModelCreating(modelBuilder);
    }
}
