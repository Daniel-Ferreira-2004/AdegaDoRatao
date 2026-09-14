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

        // Executa todas as consultas em paralelo
        var vendasHojeTask = sales.ContarPorPeriodoAsync(hoje, hoje.AddDays(1).AddTicks(-1), cancellationToken);
        var vendasMesTask = sales.ContarPorPeriodoAsync(inicioMes, fimMes, cancellationToken);
        var faturamentoHojeTask = sales.SomarTotalPorPeriodoAsync(hoje, hoje.AddDays(1).AddTicks(-1), cancellationToken);
        var faturamentoMesTask = sales.SomarTotalPorPeriodoAsync(inicioMes, fimMes, cancellationToken);
        var despesasMesTask = transactions.SomarPorTipoEPeriodoAsync(FinancialTransactionType.Saida, inicioMes, fimMes, cancellationToken);
        var entradasMesTask = transactions.SomarPorTipoEPeriodoAsync(FinancialTransactionType.Entrada, inicioMes, fimMes, cancellationToken);
        var estoqueBaixoTask = products.ListarComEstoqueBaixoAsync(cancellationToken);
        var semEstoqueTask = products.ListarSemEstoqueAsync(cancellationToken);
        var lucroBrutoTask = sales.SomarLucroBrutoEstimadoAsync(inicioMes, fimMes, cancellationToken);
        var vendasRecentesTask = sales.ListarRecentesAsync(5, cancellationToken);
        var maisVendidosTask = sales.ListarMaisVendidosAsync(inicioMes, fimMes, 5, cancellationToken);
        var formasPagamentoTask = sales.ListarFormasPagamentoMaisUsadasAsync(inicioMes, fimMes, 5, cancellationToken);

        await Task.WhenAll(
            vendasHojeTask, vendasMesTask, faturamentoHojeTask, faturamentoMesTask,
            despesasMesTask, entradasMesTask, estoqueBaixoTask, semEstoqueTask,
            lucroBrutoTask, vendasRecentesTask, maisVendidosTask, formasPagamentoTask);

        // Monta o resumo
        var summary = new DashboardSummaryResponse(
            VendasHoje: vendasHojeTask.Result,
            VendasMes: vendasMesTask.Result,
            FaturamentoHoje: faturamentoHojeTask.Result,
            FaturamentoMes: faturamentoMesTask.Result,
            DespesasMes: despesasMesTask.Result,
            SaldoMes: entradasMesTask.Result - despesasMesTask.Result,
            ProdutosEstoqueBaixo: estoqueBaixoTask.Result.Count,
            ProdutosSemEstoque: semEstoqueTask.Result.Count,
            LucroBrutoEstimadoMes: lucroBrutoTask.Result);

        // Vendas recentes (precisa do nome da forma de pagamento)
        var paymentMethodIds = vendasRecentesTask.Result.Select(x => x.PaymentMethodId).Distinct().ToArray();
        var paymentMethodNames = new Dictionary<Guid, string>();
        foreach (var pmId in paymentMethodIds)
        {
            var pm = await paymentMethods.ObterPorIdAsync(pmId, cancellationToken);
            paymentMethodNames[pmId] = pm?.Name ?? "—";
        }

        var recentSales = vendasRecentesTask.Result
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
        var topProducts = maisVendidosTask.Result
            .Select(x => new TopSellingProductResponse(x.ProductId, x.ProductName, x.Sku, x.TotalQuantity, x.TotalRevenue))
            .ToArray();

        // Top formas de pagamento
        var topPaymentMethods = formasPagamentoTask.Result
            .Select(x => new TopPaymentMethodResponse(x.PaymentMethodId, x.PaymentMethodName, x.TotalSales, x.TotalAmount))
            .ToArray();

        return new DashboardResponse(summary, recentSales, recentMovements, topProducts, topPaymentMethods);
    }
}
