using AdegaDoRatao.Domain.Common;
using AdegaDoRatao.Domain.Enums;
using AdegaDoRatao.Domain.Exceptions;

namespace AdegaDoRatao.Domain.Entities;

/// <summary>
/// Categoria de um lançamento financeiro (ex.: Vendas, Compras, Aluguel,
/// Energia). Configurável pelo usuário (RN item 9 do documento original).
/// </summary>
public class FinancialCategory : BaseAuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public FinancialTransactionType Type { get; private set; }

    private FinancialCategory()
    {
    }

    public FinancialCategory(string name, FinancialTransactionType type)
    {
        SetName(name);
        Type = type;
    }

    public void Rename(string newName)
    {
        SetName(newName);
        MarcarComoAtualizada();
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("O nome da categoria financeira é obrigatório.", nameof(name));
        }

        Name = name.Trim();
    }
}

/// <summary>
/// Lançamento financeiro (entrada ou saída de caixa) — RN24 a RN26.
///
/// Lançamentos oriundos de venda/compra (<see cref="ReferenceType"/>
/// preenchido) são criados automaticamente pelo SaleService/PurchaseService
/// e não devem ser editados diretamente (RN25) — apenas estornados através
/// do cancelamento da venda/compra de origem, o que gera um NOVO lançamento
/// de estorno (nunca edita o original, preservando a trilha de auditoria).
/// </summary>
public class FinancialTransaction : BaseEntity
{
    public string Description { get; private set; } = string.Empty;
    public FinancialTransactionType Type { get; private set; }
    public Guid FinancialCategoryId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime Date { get; private set; }
    public Guid? PaymentMethodId { get; private set; }
    public FinancialTransactionStatus Status { get; private set; }
    public Guid UserId { get; private set; }
    public string? ReferenceType { get; private set; } // "Sale" | "Purchase" | null
    public Guid? ReferenceId { get; private set; }
    public string? Notes { get; private set; }

    private FinancialTransaction()
    {
    }

    private FinancialTransaction(
        string description, FinancialTransactionType type, Guid financialCategoryId,
        decimal amount, DateTime date, Guid? paymentMethodId, Guid userId,
        string? referenceType, Guid? referenceId, string? notes)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("A descrição do lançamento financeiro é obrigatória.", nameof(description));
        }

        if (amount <= 0)
        {
            throw new ValorInvalidoException("O valor do lançamento financeiro deve ser maior que zero.");
        }

        Description = description.Trim();
        Type = type;
        FinancialCategoryId = financialCategoryId;
        Amount = amount;
        Date = date;
        PaymentMethodId = paymentMethodId;
        UserId = userId;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Status = FinancialTransactionStatus.Pago;
    }

    /// <summary>Lançamento manual (despesa, outro recebimento), criado por GERENTE/ADMIN (RN26).</summary>
    public static FinancialTransaction CriarManual(
        string description, FinancialTransactionType type, Guid financialCategoryId,
        decimal amount, DateTime date, Guid? paymentMethodId, Guid userId, string? notes)
        => new(description, type, financialCategoryId, amount, date, paymentMethodId, userId, null, null, notes);

    /// <summary>Lançamento automático gerado por uma venda concluída (RN20).</summary>
    public static FinancialTransaction CriarDeVenda(
        Guid saleId, decimal amount, DateTime date, Guid financialCategoryId, Guid? paymentMethodId, Guid userId)
        => new("Venda concluída", FinancialTransactionType.Entrada, financialCategoryId, amount, date,
               paymentMethodId, userId, "Sale", saleId, null);

    /// <summary>Lançamento automático gerado por uma compra confirmada (RN15).</summary>
    public static FinancialTransaction CriarDeCompra(
        Guid purchaseId, decimal amount, DateTime date, Guid financialCategoryId, Guid? paymentMethodId, Guid userId)
        => new("Compra recebida", FinancialTransactionType.Saida, financialCategoryId, amount, date,
               paymentMethodId, userId, "Purchase", purchaseId, null);

    /// <summary>Lançamento de estorno gerado pelo cancelamento de uma venda (RN21).</summary>
    public static FinancialTransaction CriarEstornoDeVenda(
        Guid saleId, decimal amount, DateTime date, Guid financialCategoryId, Guid userId)
        => new("Estorno de venda cancelada", FinancialTransactionType.Saida, financialCategoryId, amount, date,
               null, userId, "Sale", saleId, "Gerado automaticamente pelo cancelamento da venda.");

    /// <summary>Lançamento de estorno gerado pelo cancelamento de uma compra (RN17).</summary>
    public static FinancialTransaction CriarEstornoDeCompra(
        Guid purchaseId, decimal amount, DateTime date, Guid financialCategoryId, Guid userId)
        => new("Estorno de compra cancelada", FinancialTransactionType.Entrada, financialCategoryId, amount, date,
               null, userId, "Purchase", purchaseId, "Gerado automaticamente pelo cancelamento da compra.");

    public void Estornar()
    {
        if (Status == FinancialTransactionStatus.Estornado)
        {
            throw new TransicaoDeStatusInvalidaException("Este lançamento já está estornado.");
        }

        Status = FinancialTransactionStatus.Estornado;
        MarcarComoAtualizada();
    }
}
