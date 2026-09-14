using AdegaDoRatao.Domain.Common;
using AdegaDoRatao.Domain.Enums;
using AdegaDoRatao.Domain.Exceptions;

namespace AdegaDoRatao.Domain.Entities;

/// <summary>
/// Item de uma venda: um produto, quantidade e o preço unitário
/// "congelado" no momento da venda (RN18) — mesmo que o preço do produto
/// mude depois, este valor não é afetado.
/// </summary>
public class SaleItem : BaseEntity
{
    public Guid SaleId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal Subtotal => Quantity * UnitPrice;

    private SaleItem()
    {
    }

    public SaleItem(Guid productId, int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
        {
            throw new QuantidadeInvalidaException("A quantidade do item de venda deve ser maior que zero.");
        }

        if (unitPrice < 0)
        {
            throw new ValorInvalidoException("O preço unitário do item de venda não pode ser negativo.");
        }

        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    internal void VincularVenda(Guid saleId) => SaleId = saleId;
}

/// <summary>
/// Venda realizada no balcão da adega (RN18 a RN22).
/// Aggregate Root — SaleItem só existe dentro de uma Sale.
/// </summary>
public class Sale : BaseEntity
{
    private readonly List<SaleItem> _items = new();

    /// <summary>Número sequencial e único da venda, para exibição ao usuário (ex.: cupom).</summary>
    public long SaleNumber { get; private set; }

    public DateTime Date { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Discount { get; private set; }
    public Guid PaymentMethodId { get; private set; }
    public SaleStatus Status { get; private set; }

    public IReadOnlyCollection<SaleItem> Items => _items.AsReadOnly();

    /// <summary>Total = soma dos subtotais dos itens - desconto.</summary>
    public decimal Total => Math.Max(0, _items.Sum(i => i.Subtotal) - Discount);

    private Sale()
    {
    }

    /// <summary>
    /// O número da venda (<paramref name="saleNumber"/>) é calculado pela
    /// Application (próximo número sequencial), pois depende de consultar
    /// o banco — não é uma responsabilidade do domínio puro.
    /// </summary>
    public Sale(long saleNumber, DateTime date, Guid userId, decimal discount, Guid paymentMethodId)
    {
        if (discount < 0)
        {
            throw new ValorInvalidoException("O desconto da venda não pode ser negativo.");
        }

        SaleNumber = saleNumber;
        Date = date;
        UserId = userId;
        Discount = discount;
        PaymentMethodId = paymentMethodId;
        Status = SaleStatus.Concluida;
    }

    public void AdicionarItem(SaleItem item)
    {
        item.VincularVenda(Id);
        _items.Add(item);
    }

    /// <summary>
    /// Cancela a venda (RN21). Assim como em Purchase, este método só
    /// muda o status — o estorno de estoque e o lançamento financeiro de
    /// estorno são orquestrados pelo SaleService, na Application.
    /// </summary>
    public void Cancelar()
    {
        if (Status == SaleStatus.Cancelada)
        {
            throw new TransicaoDeStatusInvalidaException("Esta venda já está cancelada.");
        }

        Status = SaleStatus.Cancelada;
        MarcarComoAtualizada();
    }
}
