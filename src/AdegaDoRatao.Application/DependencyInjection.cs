using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AdegaDoRatao.Application;

/// <summary>Ponto único de registro da camada de Application no container de DI.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // ProductService é um tipo concreto desta camada; usá-lo como âncora
        // permite localizar todos os validadores sem usar a classe estática.
        services.AddValidatorsFromAssemblyContaining<ProductService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<IPaymentMethodService, PaymentMethodService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IFinancialService, FinancialService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        services.AddScoped<IPrecoMercadoService, PrecoMercadoService>();
        services.AddScoped<IMercadoService, MercadoService>();
        return services;
    }
}
