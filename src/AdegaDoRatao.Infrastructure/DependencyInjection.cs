using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Infrastructure.Auth;
using AdegaDoRatao.Infrastructure.Logging;
using AdegaDoRatao.Infrastructure.Persistence;
using AdegaDoRatao.Infrastructure.Repositories;
using AdegaDoRatao.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AdegaDoRatao.Infrastructure;

/// <summary>
/// Ponto único para registrar implementações técnicas. A API chamará este
/// método na composição da aplicação, sem conhecer classes concretas.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        // PostgreSQL (Npgsql): funciona com Supabase, Neon, Railway, Azure
        // Database for PostgreSQL ou qualquer Postgres local/Docker.
        if (!string.IsNullOrWhiteSpace(connectionString)) services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddHttpContextAccessor();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>(); services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IProductRepository, ProductRepository>(); services.AddScoped<ICategoryRepository, CategoryRepository>(); services.AddScoped<IBrandRepository, BrandRepository>(); services.AddScoped<IStockMovementRepository, StockMovementRepository>(); services.AddScoped<ISupplierRepository, SupplierRepository>(); services.AddScoped<IPurchaseRepository, PurchaseRepository>(); services.AddScoped<ISaleRepository, SaleRepository>(); services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>(); services.AddScoped<IFinancialCategoryRepository, FinancialCategoryRepository>(); services.AddScoped<IFinancialTransactionRepository, FinancialTransactionRepository>(); services.AddScoped<IUserRepository, UserRepository>(); services.AddScoped<IRoleRepository, RoleRepository>(); services.AddScoped<IPermissionRepository, PermissionRepository>(); services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        return services;
    }
}
