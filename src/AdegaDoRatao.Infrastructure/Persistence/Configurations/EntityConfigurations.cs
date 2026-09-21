using AdegaDoRatao.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdegaDoRatao.Infrastructure.Persistence.Configurations;

internal static class Mapping
{
    public static void Base<T>(EntityTypeBuilder<T> b, string table) where T : AdegaDoRatao.Domain.Common.BaseEntity
    { b.ToTable(table); b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever(); b.Property(x => x.CreatedAt).IsRequired(); }
    public static void Auditable<T>(EntityTypeBuilder<T> b, string table) where T : AdegaDoRatao.Domain.Common.BaseAuditableEntity
    { Base(b, table); b.Property(x => x.IsActive).IsRequired(); }
}

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{ public void Configure(EntityTypeBuilder<Product> b) { Mapping.Auditable(b,"Products"); b.Property(x=>x.Name).HasMaxLength(150).IsRequired(); b.Property(x=>x.Sku).HasMaxLength(60).IsRequired(); b.HasIndex(x=>x.Sku).IsUnique(); b.Property(x=>x.Barcode).HasMaxLength(100); b.HasIndex(x=>x.Barcode).IsUnique().HasFilter("\"Barcode\" IS NOT NULL"); b.Property(x=>x.UnitOfMeasure).HasMaxLength(10).IsRequired(); b.Property(x=>x.CostPrice).HasPrecision(18,2); b.Property(x=>x.SalePrice).HasPrecision(18,2); b.Property(x=>x.RowVersion).IsRowVersion(); b.HasOne<Category>().WithMany().HasForeignKey(x=>x.CategoryId).OnDelete(DeleteBehavior.Restrict); b.HasOne<Brand>().WithMany().HasForeignKey(x=>x.BrandId).OnDelete(DeleteBehavior.Restrict); b.ToTable(t=>t.HasCheckConstraint("CK_Product_Prices","\"CostPrice\" >= 0 AND \"SalePrice\" >= 0")); } }
public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{ public void Configure(EntityTypeBuilder<Category> b) { Mapping.Auditable(b,"Categories"); b.Property(x=>x.Name).HasMaxLength(100).IsRequired(); b.HasIndex(x=>x.Name).IsUnique(); } }
public sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{ public void Configure(EntityTypeBuilder<Brand> b) { Mapping.Auditable(b,"Brands"); b.Property(x=>x.Name).HasMaxLength(100).IsRequired(); b.HasIndex(x=>x.Name).IsUnique(); } }
public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{ public void Configure(EntityTypeBuilder<Supplier> b) { Mapping.Auditable(b,"Suppliers"); b.Property(x=>x.Name).HasMaxLength(150).IsRequired(); b.Property(x=>x.Document).HasMaxLength(20); b.Property(x=>x.Email).HasMaxLength(254); } }
public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{ public void Configure(EntityTypeBuilder<StockMovement> b) { Mapping.Base(b,"StockMovements"); b.Property(x=>x.Reason).HasMaxLength(500).IsRequired(); b.Property(x=>x.ReferenceType).HasMaxLength(30); b.HasIndex(x=>new{x.ProductId,x.CreatedAt}); b.HasOne<Product>().WithMany().HasForeignKey(x=>x.ProductId).OnDelete(DeleteBehavior.Restrict); b.HasOne<User>().WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Restrict); } }
public sealed class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{ public void Configure(EntityTypeBuilder<Purchase> b) { Mapping.Base(b,"Purchases"); b.Property(x=>x.Discount).HasPrecision(18,2); b.Property(x=>x.Freight).HasPrecision(18,2); b.Ignore(x=>x.Total); b.HasMany(x=>x.Items).WithOne().HasForeignKey(x=>x.PurchaseId).OnDelete(DeleteBehavior.Restrict); b.HasOne<Supplier>().WithMany().HasForeignKey(x=>x.SupplierId).OnDelete(DeleteBehavior.Restrict); b.HasOne<PaymentMethod>().WithMany().HasForeignKey(x=>x.PaymentMethodId).OnDelete(DeleteBehavior.Restrict); b.HasOne<User>().WithMany().HasForeignKey(x=>x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict); } }
public sealed class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{ public void Configure(EntityTypeBuilder<PurchaseItem> b) { Mapping.Base(b,"PurchaseItems"); b.Property(x=>x.UnitCost).HasPrecision(18,2); b.Ignore(x=>x.Subtotal); b.HasOne<Product>().WithMany().HasForeignKey(x=>x.ProductId).OnDelete(DeleteBehavior.Restrict); } }
public sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{ public void Configure(EntityTypeBuilder<Sale> b) { Mapping.Base(b,"Sales"); b.HasIndex(x=>x.SaleNumber).IsUnique(); b.HasIndex(x=>x.Date); b.Property(x=>x.Discount).HasPrecision(18,2); b.Ignore(x=>x.Total); b.HasMany(x=>x.Items).WithOne().HasForeignKey(x=>x.SaleId).OnDelete(DeleteBehavior.Restrict); b.HasOne<User>().WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Restrict); b.HasOne<PaymentMethod>().WithMany().HasForeignKey(x=>x.PaymentMethodId).OnDelete(DeleteBehavior.Restrict); } }
public sealed class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{ public void Configure(EntityTypeBuilder<SaleItem> b) { Mapping.Base(b,"SaleItems"); b.Property(x=>x.UnitPrice).HasPrecision(18,2); b.Ignore(x=>x.Subtotal); b.HasOne<Product>().WithMany().HasForeignKey(x=>x.ProductId).OnDelete(DeleteBehavior.Restrict); } }
public sealed class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{ public void Configure(EntityTypeBuilder<PaymentMethod> b) { Mapping.Auditable(b,"PaymentMethods"); b.Property(x=>x.Name).HasMaxLength(80).IsRequired(); b.HasIndex(x=>x.Name).IsUnique(); } }
public sealed class FinancialCategoryConfiguration : IEntityTypeConfiguration<FinancialCategory>
{ public void Configure(EntityTypeBuilder<FinancialCategory> b) { Mapping.Auditable(b,"FinancialCategories"); b.Property(x=>x.Name).HasMaxLength(100).IsRequired(); b.HasIndex(x=>x.Name).IsUnique(); } }
public sealed class FinancialTransactionConfiguration : IEntityTypeConfiguration<FinancialTransaction>
{ public void Configure(EntityTypeBuilder<FinancialTransaction> b) { Mapping.Base(b,"FinancialTransactions"); b.Property(x=>x.Description).HasMaxLength(250).IsRequired(); b.Property(x=>x.Amount).HasPrecision(18,2); b.Property(x=>x.ReferenceType).HasMaxLength(30); b.Property(x=>x.Notes).HasMaxLength(1000); b.HasIndex(x=>new{x.Date,x.Type}); b.HasOne<FinancialCategory>().WithMany().HasForeignKey(x=>x.FinancialCategoryId).OnDelete(DeleteBehavior.Restrict); b.HasOne<User>().WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Restrict); b.HasOne<PaymentMethod>().WithMany().HasForeignKey(x=>x.PaymentMethodId).OnDelete(DeleteBehavior.Restrict); } }
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{ public void Configure(EntityTypeBuilder<User> b) { Mapping.Auditable(b,"Users"); b.Property(x=>x.Name).HasMaxLength(150).IsRequired(); b.Property(x=>x.Email).HasMaxLength(254).IsRequired(); b.HasIndex(x=>x.Email).IsUnique(); b.Property(x=>x.PasswordHash).HasMaxLength(255).IsRequired(); b.HasOne<Role>().WithMany().HasForeignKey(x=>x.RoleId).OnDelete(DeleteBehavior.Restrict); } }
public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{ public void Configure(EntityTypeBuilder<Role> b) { Mapping.Base(b,"Roles"); b.Property(x=>x.Name).HasMaxLength(50).IsRequired(); b.HasIndex(x=>x.Name).IsUnique(); } }
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{ public void Configure(EntityTypeBuilder<RefreshToken> b) { Mapping.Base(b,"RefreshTokens"); b.Property(x=>x.TokenHash).HasMaxLength(64).IsRequired(); b.HasIndex(x=>x.TokenHash).IsUnique(); b.HasIndex(x=>x.UserId); b.HasOne<User>().WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Cascade); } }
public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{ public void Configure(EntityTypeBuilder<Permission> b) { Mapping.Base(b,"Permissions"); b.Property(x=>x.Code).HasMaxLength(100).IsRequired(); b.HasIndex(x=>x.Code).IsUnique(); b.Property(x=>x.Description).HasMaxLength(250); } }
public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{ public void Configure(EntityTypeBuilder<RolePermission> b) { b.ToTable("RolePermissions"); b.HasKey(x=>new{x.RoleId,x.PermissionId}); b.HasOne<Role>().WithMany(r=>r.RolePermissions).HasForeignKey(x=>x.RoleId).OnDelete(DeleteBehavior.Restrict); b.HasOne<Permission>().WithMany().HasForeignKey(x=>x.PermissionId).OnDelete(DeleteBehavior.Restrict); } }
public sealed class MarketConfiguration : IEntityTypeConfiguration<Market>
{ public void Configure(EntityTypeBuilder<Market> b) { Mapping.Auditable(b,"Markets"); b.Property(x=>x.Name).HasMaxLength(150).IsRequired(); b.Property(x=>x.Rede).HasMaxLength(80).IsRequired(); b.Property(x=>x.Address).HasMaxLength(250).IsRequired(); b.Property(x=>x.City).HasMaxLength(100); b.Property(x=>x.State).HasMaxLength(2); b.Property(x=>x.ZipCode).HasMaxLength(9); b.HasIndex(x=>new{x.Latitude,x.Longitude}); } }
public sealed class MarketPriceSnapshotConfiguration : IEntityTypeConfiguration<MarketPriceSnapshot>
{ public void Configure(EntityTypeBuilder<MarketPriceSnapshot> b) { Mapping.Base(b,"MarketPriceSnapshots"); b.Property(x=>x.Ean).HasMaxLength(20).IsRequired(); b.Property(x=>x.Rede).HasMaxLength(80).IsRequired(); b.Property(x=>x.NomeProdutoNaRede).HasMaxLength(200); b.Property(x=>x.Preco).HasPrecision(18,2); b.HasIndex(x=>new{x.Ean,x.Rede}).IsUnique(); } }
public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{ public void Configure(EntityTypeBuilder<AuditLog> b) { Mapping.Base(b,"AuditLogs"); b.Property(x=>x.Action).HasMaxLength(60).IsRequired(); b.Property(x=>x.EntityName).HasMaxLength(100).IsRequired(); b.Property(x=>x.EntityId).HasMaxLength(50).IsRequired(); b.Property(x=>x.OldValues).HasColumnType("nvarchar(max)"); b.Property(x=>x.NewValues).HasColumnType("nvarchar(max)"); b.HasOne<User>().WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Restrict); } }