using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Enums;
using AdegaDoRatao.Domain.Interfaces;

namespace AdegaDoRatao.Application.Services;

/// <summary>
/// Casos de uso do módulo financeiro (RN24–RN27).
///
/// Responsabilidades:
/// - CRUD de categorias financeiras (configuráveis pelo usuário).
/// - Lançamentos manuais (despesas, outros recebimentos).
/// - Estorno de lançamentos (gera novo lançamento, nunca edita o original).
/// - Fluxo de caixa (entradas, saídas e saldo por período).
///
/// Lançamentos automáticos de venda/compra são criados pelos respectivos
/// serviços (SaleService/PurchaseService), não por aqui.
/// </summary>
public sealed class FinancialService(
    IFinancialCategoryRepository categories,
    IFinancialTransactionRepository transactions,
    IPaymentMethodRepository paymentMethods,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IAuditService audit) : IFinancialService
{
    // ========================================================================
    // Categorias financeiras
    // ========================================================================

    public async Task<FinancialCategoryResponse> CreateCategoryAsync(
        CreateFinancialCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = new FinancialCategory(request.Name, request.Type);
        await categories.AdicionarAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await audit.RegisterAsync("CREATE", nameof(FinancialCategory), category.Id.ToString(),
            null, ToCategoryResponse(category), cancellationToken);
        return ToCategoryResponse(category);
    }

    public async Task<IReadOnlyList<FinancialCategoryResponse>> ListCategoriesAsync(
        CancellationToken cancellationToken = default)
        => (await categories.ListarTodosAsync(cancellationToken))
            .Select(ToCategoryResponse)
            .ToArray();

    public async Task SetCategoryActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default)
    {
        var category = await categories.ObterPorIdAsync(id, cancellationToken)
            ?? throw new UseCaseException("Categoria financeira não encontrada.");

        if (active) category.Ativar(); else category.Desativar();
        categories.Atualizar(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await audit.RegisterAsync(active ? "ACTIVATE" : "DEACTIVATE", nameof(FinancialCategory),
            id.ToString(), null, new { category.IsActive }, cancellationToken);
    }

    // ========================================================================
    // Lançamentos manuais
    // ========================================================================

    public async Task<FinancialTransactionResponse> CreateTransactionAsync(
        CreateFinancialTransactionRequest request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId
            ?? throw new UseCaseException("Usuário autenticado não identificado.");

        var category = await categories.ObterPorIdAsync(request.FinancialCategoryId, cancellationToken)
            ?? throw new UseCaseException("Categoria financeira não encontrada.");
        if (!category.IsActive)
            throw new UseCaseException("A categoria financeira informada está inativa.");

        if (request.PaymentMethodId.HasValue)
        {
            var payment = await paymentMethods.ObterPorIdAsync(request.PaymentMethodId.Value, cancellationToken)
                ?? throw new UseCaseException("Forma de pagamento não encontrada.");
            if (!payment.IsActive)
                throw new UseCaseException("A forma de pagamento informada está inativa.");
        }

        var transaction = FinancialTransaction.CriarManual(
            request.Description, request.Type, request.FinancialCategoryId,
            request.Amount, request.Date, request.PaymentMethodId, userId, request.Notes);

        await transactions.AdicionarAsync(transaction, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await audit.RegisterAsync("CREATE", nameof(FinancialTransaction), transaction.Id.ToString(),
            null, ToTransactionResponse(transaction, category.Name), cancellationToken);
        return ToTransactionResponse(transaction, category.Name);
    }

    public async Task<PagedResult<FinancialTransactionResponse>> SearchTransactionsAsync(
        FinancialTransactionQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var from = query.From ?? DateTime.MinValue;
        var to = query.To ?? DateTime.MaxValue;

        var items = await transactions.ListarPorPeriodoAsync(from, to, query.Type, cancellationToken);

        // Paginação em memória (o repositório retorna a lista filtrada)
        var total = items.Count;
        var paged = items.Skip((page - 1) * pageSize).Take(pageSize).ToArray();

        // Carrega nomes das categorias para o response
        var categoryIds = paged.Select(x => x.FinancialCategoryId).Distinct().ToArray();
        var categoryNames = new Dictionary<Guid, string>();
        foreach (var catId in categoryIds)
        {
            var cat = await categories.ObterPorIdAsync(catId, cancellationToken);
            categoryNames[catId] = cat?.Name ?? "—";
        }

        var responses = paged.Select(t => ToTransactionResponse(t, categoryNames.GetValueOrDefault(t.FinancialCategoryId, "—"))).ToArray();
        return new PagedResult<FinancialTransactionResponse>(responses, page, pageSize, total);
    }

    public async Task EstornarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await transactions.ObterPorIdAsync(id, cancellationToken)
            ?? throw new UseCaseException("Lançamento financeiro não encontrado.");

        if (transaction.Status == FinancialTransactionStatus.Estornado)
            throw new UseCaseException("Este lançamento já está estornado.");

        // RN25: lançamentos automáticos (de venda/compra) não podem ser
        // estornados manualmente — apenas pelo cancelamento da origem.
        if (transaction.ReferenceType is not null)
            throw new UseCaseException(
                "Lançamentos gerados automaticamente por venda/compra não podem ser estornados manualmente. " +
                "Cancele a venda/compra de origem para gerar o estorno automático.");

        transaction.Estornar();
        transactions.Atualizar(transaction);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await audit.RegisterAsync("ESTORNO", nameof(FinancialTransaction), id.ToString(),
            null, new { transaction.Status }, cancellationToken);
    }

    // ========================================================================
    // Fluxo de caixa
    // ========================================================================

    public async Task<CashFlowResponse> GetCashFlowAsync(
        DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var entradas = await transactions.SomarPorTipoEPeriodoAsync(
            FinancialTransactionType.Entrada, from, to, cancellationToken);
        var saidas = await transactions.SomarPorTipoEPeriodoAsync(
            FinancialTransactionType.Saida, from, to, cancellationToken);

        return new CashFlowResponse(from, to, entradas, saidas, entradas - saidas);
    }

    // ========================================================================
    // Mapeadores
    // ========================================================================

    private static FinancialCategoryResponse ToCategoryResponse(FinancialCategory c)
        => new(c.Id, c.Name, c.Type, c.IsActive, c.CreatedAt, c.UpdatedAt);

    private static FinancialTransactionResponse ToTransactionResponse(FinancialTransaction t, string categoryName)
        => new(t.Id, t.Description, t.Type, t.FinancialCategoryId, categoryName,
               t.Amount, t.Date, t.PaymentMethodId, t.Status, t.UserId,
               t.ReferenceType, t.ReferenceId, t.Notes, t.CreatedAt);
}
