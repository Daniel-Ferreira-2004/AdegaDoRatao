using AdegaDoRatao.Domain.Enums;

namespace AdegaDoRatao.Application.DTOs;

// ============================================================================
// Categorias financeiras (RN26): cadastro configurável pelo usuário.
// ============================================================================

/// <summary>Criação de uma categoria financeira (ex.: "Aluguel", "Energia").</summary>
public sealed record CreateFinancialCategoryRequest(string Name, FinancialTransactionType Type);

/// <summary>Resposta de uma categoria financeira.</summary>
public sealed record FinancialCategoryResponse(Guid Id, string Name, FinancialTransactionType Type,
    bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);

// ============================================================================
// Lançamentos financeiros (RN24–RN27).
// ============================================================================

/// <summary>
/// Criação de um lançamento manual (RN26). Lançamentos automáticos de
/// venda/compra NÃO passam por aqui — são gerados pelos respectivos serviços.
/// </summary>
public sealed record CreateFinancialTransactionRequest(
    string Description,
    FinancialTransactionType Type,
    Guid FinancialCategoryId,
    decimal Amount,
    DateTime Date,
    Guid? PaymentMethodId,
    string? Notes);

/// <summary>Detalhe de um lançamento financeiro.</summary>
public sealed record FinancialTransactionResponse(
    Guid Id,
    string Description,
    FinancialTransactionType Type,
    Guid FinancialCategoryId,
    string FinancialCategoryName,
    decimal Amount,
    DateTime Date,
    Guid? PaymentMethodId,
    FinancialTransactionStatus Status,
    Guid UserId,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Notes,
    DateTime CreatedAt);

/// <summary>Filtro de listagem de lançamentos por período e tipo.</summary>
public sealed record FinancialTransactionQuery(
    DateTime? From,
    DateTime? To,
    FinancialTransactionType? Type,
    int Page = 1,
    int PageSize = 20);

/// <summary>Resumo do fluxo de caixa de um período (RN27).</summary>
public sealed record CashFlowResponse(
    DateTime From,
    DateTime To,
    decimal TotalEntradas,
    decimal TotalSaidas,
    decimal Saldo);