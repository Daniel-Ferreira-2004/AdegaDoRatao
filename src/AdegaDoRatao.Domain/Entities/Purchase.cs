using AdegaDoRatao.Domain.Common;
using AdegaDoRatao.Domain.Enums;
using AdegaDoRatao.Domain.Exceptions;

namespace AdegaDoRatao.Domain.Entities;

/// <summary>
/// Item de uma compra: um produto, sua quantidade e o custo unitário
/// pago naquela compra (RN13).
/// </summary>
public class PurchaseItem : BaseEntity
{
    public Guid PurchaseId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitCost { get; private set; }

    /// <summary>Subtotal = Quantity * UnitCost.</summary>
    public decimal Subtotal => Quantity * UnitCost;

    private PurchaseItem()
    {
    }

    public PurchaseItem(Guid productId, int quantity, decimal unitCost)
    {
        if (quantity <= 0)
        {
            throw new QuantidadeInvalidaException("A quantidade do item de compra deve ser maior que zero.");
        }

        if (unitCost < 0)
        {
            throw new ValorInvalidoException("O custo unitário do item de compra não pode ser negativo.");
        }

        ProductId = productId;
        Quantity = quantity;
        UnitCost = unitCost;
    }

    /// <summary>Usado pelo EF Core para preencher a FK ao materializar o agregado.</summary>
    internal void VincularCompra(Guid purchaseId) => PurchaseId = purchaseId;
}

/// <summary>
/// Compra de produtos junto a um fornecedor (RN13/RN15/RN16/RN17).
///
/// Purchase é um Aggregate Root: PurchaseItem só existe dentro de uma
/// Purchase e é sempre manipulado através dela (nunca criado/alterado
/// isoladamente pela Application).
/// </summary>
public class Purchase : BaseEntity
{
    private readonly List<PurchaseItem> _items = new();

    public Guid SupplierId { get; private set; }
    public DateTime Date { get; private set; }
    public decimal Discount { get; private set; }
    public decimal Freight { get; private set; }
    public Guid PaymentMethodId { get; private set; }
    public PurchaseStatus Status { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    public IReadOnlyCollection<PurchaseItem> Items => _items.AsReadOnly();

    /// <summary>Total = soma dos subtotais dos itens - desconto + frete (RN14).</summary>
    public decimal Total => _items.Sum(i => i.Subtotal) - Discount + Freight;

    private Purchase()
    {
    }

    public Purchase(Guid supplierId, DateTime date, decimal discount, decimal freight,
        Guid paymentMethodId, Guid createdByUserId)
    {
        if (discount < 0)
        {
            throw new ValorInvalidoException("O desconto da compra não pode ser negativo.");
        }

        if (freight < 0)
        {
            throw new ValorInvalidoException("O frete da compra não pode ser negativo.");
        }

        SupplierId = supplierId;
        Date = date;
        Discount = discount;
        Freight = freight;
        PaymentMethodId = paymentMethodId;
        CreatedByUserId = createdByUserId;
        Status = PurchaseStatus.Pendente;
    }

    public void AdicionarItem(PurchaseItem item)
    {
        if (Status != PurchaseStatus.Pendente)
        {
            throw new TransicaoDeStatusInvalidaException(
                "Só é possível adicionar itens a uma compra com status Pendente.");
        }

        item.VincularCompra(Id);
        _items.Add(item);
    }

    /// <summary>
    /// Confirma o recebimento da compra (RN15). Este método só altera o
    /// status — a orquestração de estoque e financeiro (que envolve outras
    /// entidades/agregados) acontece no PurchaseService, na Application,
    /// dentro de uma transação de banco.
    /// </summary>
    public void ConfirmarRecebimento()
    {
        if (Status != PurchaseStatus.Pendente)
        {
            throw new TransicaoDeStatusInvalidaException(
                $"Não é possível confirmar uma compra com status '{Status}'.");
        }

        if (_items.Count == 0)
        {
            throw new TransicaoDeStatusInvalidaException(
                "Não é possível confirmar uma compra sem itens.");
        }

        Status = PurchaseStatus.Recebida;
        MarcarComoAtualizada();
    }

    /// <summary>
    /// Cancela a compra (RN17). Se ainda estava Pendente, é um cancelamento
    /// simples. Se já estava Recebida, o PurchaseService deve, ANTES de
    /// chamar este método, validar se o estorno de estoque é possível
    /// (estoque atual >= quantidade recebida) — ver EstornoNaoPermitidoException.
    /// </summary>
    public void Cancelar()
    {
        if (Status == PurchaseStatus.Cancelada)
        {
            throw new TransicaoDeStatusInvalidaException("Esta compra já está cancelada.");
        }

        Status = PurchaseStatus.Cancelada;
        MarcarComoAtualizada();
    }
}
