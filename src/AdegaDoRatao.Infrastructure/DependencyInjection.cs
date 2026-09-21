
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Application.Services;
using AdegaDoRatao.Domain.Interfaces;
using AdegaDoRatao.Infrastructure.Auth;
using AdegaDoRatao.Infrastructure.ExternalServices;
using AdegaDoRatao.Infrastructure.ExternalServices.Coletores;
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
        // MERCADOS (filtro de mercados próximos)
        // ============================================================

        services.AddScoped<IMarketRepository, MarketRepository>();

        services.AddScoped<IMarketPriceSnapshotRepository, MarketPriceSnapshotRepository>();


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

        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();


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
        // INTEGRAÇÃO EXTERNA — GEOCODIFICAÇÃO (Nominatim/OpenStreetMap)
        // ============================================================

        // Nominatim é gratuito e não exige chave. Se migrar para provedor
        // pago (ex.: Google), a chave vai em User Secrets ("Geocoding:ApiKey")
        // ou variável de ambiente "Geocoding__ApiKey" — nunca no appsettings.
        services.Configure<GeocodingOptions>(
            configuration.GetSection(GeocodingOptions.SectionName));

        // O provedor é escolhido por configuração (Geocoding:Provider):
        //  - "Nominatim" (padrão): gratuito, sem chave;
        //  - "Google": Google Maps Geocoding API — exige Geocoding:ApiKey
        //    em User Secrets ou variável de ambiente (nunca no appsettings).
        services.AddHttpClient<NominatimGeocodingService>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<GeocodingOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddHttpClient<GoogleMapsGeocodingService>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<GeocodingOptions>>().Value;
            client.BaseAddress = new Uri(options.GoogleBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddScoped<IGeocodingService>(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<GeocodingOptions>>().Value;

            return options.Provider.Equals("Google", StringComparison.OrdinalIgnoreCase)
                ? serviceProvider.GetRequiredService<GoogleMapsGeocodingService>()
                : (IGeocodingService)serviceProvider.GetRequiredService<NominatimGeocodingService>();
        });

        // ============================================================
        // AGENTE DE PREÇOS — COLETORES POR REDE + JOB DIÁRIO
        // ============================================================

        // Coletores rápidos (HTTP direto, sem navegador). Cada um é
        // registrado como tipo CONCRETO com seu próprio HttpClient —
        // registrar dois AddHttpClient<IPrecoRedeCollector, ...> faria a
        // configuração do segundo sobrescrever a do primeiro (mesmo tipo
        // de serviço), mandando o Tenda chamar a URL do Atacadão.
        services.AddHttpClient<TendaPrecoCollector>(TendaPrecoCollectorSetup.Configurar);
        services.AddHttpClient<AtacadaoPrecoCollector>(AtacadaoPrecoCollectorSetup.Configurar);

        // Coletores via Playwright (navegador real) — lentos, só no job diário.
        // Requer 'playwright install chromium' após o build (ver docs/19-AGENTE-PRECOS.md).
        services.AddScoped<ShibataPrecoCollector>();
        services.AddScoped<SondaPrecoCollector>();

        // Expõe todos como IPrecoRedeCollector para o IEnumerable<> do serviço.
        services.AddScoped<IPrecoRedeCollector>(sp => sp.GetRequiredService<TendaPrecoCollector>());
        services.AddScoped<IPrecoRedeCollector>(sp => sp.GetRequiredService<AtacadaoPrecoCollector>());
        services.AddScoped<IPrecoRedeCollector>(sp => sp.GetRequiredService<ShibataPrecoCollector>());
        services.AddScoped<IPrecoRedeCollector>(sp => sp.GetRequiredService<SondaPrecoCollector>());

        services.AddScoped<IAtualizadorPrecosRedesService, AtualizadorPrecosRedesService>();
        services.AddHostedService<AtualizadorPrecosRedesJob>();

        // ============================================================
        // FINALIZAÇÃO
        // ============================================================

        return services;
    }
}
