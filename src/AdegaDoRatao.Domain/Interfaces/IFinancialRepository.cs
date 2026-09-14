using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Enums;

namespace AdegaDoRatao.Domain.Interfaces;

public interface IFinancialCategoryRepository : IRepository<FinancialCategory>
{
}

public interface IFinancialTransactionRepository : IRepository<FinancialTransaction>
{
    Task<IReadOnlyList<FinancialTransaction>> ListarPorPeriodoAsync(DateTime from, DateTime to,
        FinancialTransactionType? type, CancellationToken cancellationToken = default);

    /// <summary>Usado para calcular o saldo do fluxo de caixa (RN27).</summary>
    Task<decimal> SomarPorTipoEPeriodoAsync(FinancialTransactionType type, DateTime from, DateTime to,
        CancellationToken cancellationToken = default);
}
