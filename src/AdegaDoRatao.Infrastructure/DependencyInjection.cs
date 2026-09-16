
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Interfaces;
using AdegaDoRatao.Infrastructure.Auth;
using AdegaDoRatao.Infrastructure.ExternalServices;
using AdegaDoRatao.Infrastructure.Logging;
using AdegaDoRatao.Infrastructure.Persistence;
using AdegaDoRatao.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AdegaDoRatao.Infrastructure;

/// <summary>
/// Ponto único para registrar as implementações técnicas da Infrastructure.
/// A API chama este método na composição da aplicação, sem precisar conhecer
/// as classes concretas utilizadas pela Infrastructure.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ============================================================
        // BANCO DE DADOS
        // ============================================================

        var connectionString =
            configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A connection string 'DefaultConnection' não foi configurada. " +
                "Configure-a no appsettings.json, User Secrets ou variável de ambiente."
            );
        }

        // PostgreSQL (Npgsql)
        // Compatível com PostgreSQL local, Docker, Supabase, Neon,
        // Railway, Azure Database for PostgreSQL, entre outros.
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });


        // ============================================================
        // AUTENTICAÇÃO / JWT
        // ============================================================

        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName)
        );

        services.AddHttpContextAccessor();

        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddScoped<ICurrentUserService, CurrentUserService>();


        // ============================================================
        // UNIT OF WORK
        // ============================================================

        services.AddScoped<IUnitOfWork, UnitOfWork>();


        // ============================================================
        // AUDITORIA
        // ============================================================

        services.AddScoped<IAuditService, AuditService>();

        services.AddScoped<IAuditLogRepository, AuditLogRepository>();


        // ============================================================
        // PRODUTOS / CATEGORIAS / MARCAS
        // ============================================================

        services.AddScoped<IProductRepository, ProductRepository>();

        services.AddScoped<ICategoryRepository, CategoryRepository>();

        services.AddScoped<IBrandRepository, BrandRepository>();


        // ============================================================
        // ESTOQUE / FORNECEDORES
        // ============================================================

        services.AddScoped<IStockMovementRepository, StockMovementRepository>();

        services.AddScoped<ISupplierRepository, SupplierRepository>();


        // ============================================================
        // COMPRAS
        // ============================================================

        services.AddScoped<IPurchaseRepository, PurchaseRepository>();


        // ============================================================
        // VENDAS
        // ============================================================

        services.AddScoped<ISaleRepository, SaleRepository>();

        services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();


        // ============================================================
        // FINANCEIRO
        // ============================================================

        services.AddScoped<IFinancialCategoryRepository, FinancialCategoryRepository>();

        services.AddScoped<IFinancialTransactionRepository, FinancialTransactionRepository>();


        // ============================================================
        // USUÁRIOS / PERMISSÕES
        // ============================================================

        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<IRoleRepository, RoleRepository>();

        services.AddScoped<IPermissionRepository, PermissionRepository>();


        // ============================================================
        // INTEGRAÇÃO EXTERNA — PREÇOS DE MERCADO (Cnova Tech Data Market)
        // ============================================================

        // A API Key NUNCA fica no appsettings.json versionado: User Secrets
        // em dev ("DataMarket:ApiKey") e variável de ambiente
        // "DataMarket__ApiKey" em produção — mesmo padrão do Jwt:Secret.
        services.Configure<DataMarketOptions>(
            configuration.GetSection(DataMarketOptions.SectionName));

        // Cache em memória para proteger a cota do plano Free (50 consultas/mês).
        services.AddMemoryCache();

        services.AddHttpClient<IPrecoMercadoExternoService, DataMarketPrecoService>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<DataMarketOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        // ============================================================
        // FINALIZAÇÃO
        // ============================================================

        return services;
    }
}
