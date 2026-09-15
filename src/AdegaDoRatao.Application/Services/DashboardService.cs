using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Enums;
using AdegaDoRatao.Domain.Interfaces;

namespace AdegaDoRatao.Application.Services;

/// <summary>
/// Dashboard operacional (RF21): agrega dados de vendas, financeiro e
/// estoque em um único endpoint para o frontend.
///
/// RN27: Saldo = entradas - saídas do período.
/// RN28: Lucro bruto = (preço venda - preço custo) × quantidade.
/// RN29: Estoque baixo = > 0 e ≤ mínimo; sem estoque = 0.
/// </summary>
public sealed class DashboardService(
    ISaleRepository sales,
    IProductRepository products,
    IStockMovementRepository movements,
    IFinancialTransactionRepository transactions,
    IPaymentMethodRepository paymentMethods) : IDashboardService
{
    public async Task<DashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var hoje = DateTime.UtcNow.Date;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
        var fimMes = inicioMes.AddMonths(1).AddDays(-1);

        // Executa as consultas sequencialmente: os repositórios compartilham
        // o mesmo DbContext (scoped), que não é thread-safe.
        var vendasHoje = await sales.ContarPorPeriodoAsync(hoje, hoje.AddDays(1).AddTicks(-1), cancellationToken);
        var vendasMes = await sales.ContarPorPeriodoAsync(inicioMes, fimMes, cancellationToken);
        var faturamentoHoje = await sales.SomarTotalPorPeriodoAsync(hoje, hoje.AddDays(1).AddTicks(-1), cancellationToken);
        var faturamentoMes = await sales.SomarTotalPorPeriodoAsync(inicioMes, fimMes, cancellationToken);
        var despesasMes = await transactions.SomarPorTipoEPeriodoAsync(FinancialTransactionType.Saida, inicioMes, fimMes, cancellationToken);
        var entradasMes = await transactions.SomarPorTipoEPeriodoAsync(FinancialTransactionType.Entrada, inicioMes, fimMes, cancellationToken);
        var estoqueBaixo = await products.ListarComEstoqueBaixoAsync(cancellationToken);
        var semEstoque = await products.ListarSemEstoqueAsync(cancellationToken);
        var lucroBruto = await sales.SomarLucroBrutoEstimadoAsync(inicioMes, fimMes, cancellationToken);
        var vendasRecentes = await sales.ListarRecentesAsync(5, cancellationToken);
        var maisVendidos = await sales.ListarMaisVendidosAsync(inicioMes, fimMes, 5, cancellationToken);
        var formasPagamento = await sales.ListarFormasPagamentoMaisUsadasAsync(inicioMes, fimMes, 5, cancellationToken);

        // Monta o resumo
        var summary = new DashboardSummaryResponse(
            VendasHoje: vendasHoje,
            VendasMes: vendasMes,
            FaturamentoHoje: faturamentoHoje,
            FaturamentoMes: faturamentoMes,
            DespesasMes: despesasMes,
            SaldoMes: entradasMes - despesasMes,
            ProdutosEstoqueBaixo: estoqueBaixo.Count,
            ProdutosSemEstoque: semEstoque.Count,
            LucroBrutoEstimadoMes: lucroBruto);

        // Vendas recentes (precisa do nome da forma de pagamento)
        var paymentMethodIds = vendasRecentes.Select(x => x.PaymentMethodId).Distinct().ToArray();
        var paymentMethodNames = new Dictionary<Guid, string>();
        foreach (var pmId in paymentMethodIds)
        {
            var pm = await paymentMethods.ObterPorIdAsync(pmId, cancellationToken);
            paymentMethodNames[pmId] = pm?.Name ?? "—";
        }

        var recentSales = vendasRecentes
            .Select(s => new RecentSaleResponse(
                s.Id, s.SaleNumber, s.Date, s.Total,
                paymentMethodNames.GetValueOrDefault(s.PaymentMethodId, "—"),
                s.Items.Count))
            .ToArray();

        // Movimentações recentes (últimas 5)
        var recentMovements = (await movements.ListarRecentesComProdutoAsync(5, cancellationToken))
            .Select(x => new RecentStockMovementResponse(
                x.Movement.Id, x.ProductName, x.Movement.Type.ToString(),
                x.Movement.Quantity, x.Movement.CreatedAt, x.Movement.Reason))
            .ToArray();

        // Top produtos
        var topProducts = maisVendidos
            .Select(x => new TopSellingProductResponse(x.ProductId, x.ProductName, x.Sku, x.TotalQuantity, x.TotalRevenue))
            .ToArray();

        // Top formas de pagamento
        var topPaymentMethods = formasPagamento
            .Select(x => new TopPaymentMethodResponse(x.PaymentMethodId, x.PaymentMethodName, x.TotalSales, x.TotalAmount))
            .ToArray();

        return new DashboardResponse(summary, recentSales, recentMovements, topProducts, topPaymentMethods);
    }
}
