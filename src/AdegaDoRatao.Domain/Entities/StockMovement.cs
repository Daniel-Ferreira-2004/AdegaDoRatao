using AdegaDoRatao.Domain.Common;
using AdegaDoRatao.Domain.Enums;
using AdegaDoRatao.Domain.Exceptions;

namespace AdegaDoRatao.Domain.Entities;

/// <summary>
/// Registro imutável de uma movimentação de estoque (RN09/RN10/RN36).
/// Nunca é editado nem excluído depois de criado — é o "log" oficial de
/// tudo que aconteceu com o estoque de um produto.
/// </summary>
public class StockMovement : BaseEntity
{
    public Guid ProductId { get; private set; }
    public StockMovementType Type { get; private set; }
    public int Quantity { get; private set; }
    public int PreviousStock { get; private set; }
    public int NewStock { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }

    /// <summary>
    /// Quando a movimentação foi originada por uma venda ou compra, estes
    /// dois campos guardam a referência (ex.: ReferenceType = "Sale",
    /// ReferenceId = Id da venda), permitindo rastrear a origem sem criar
    /// uma FK obrigatória para múltiplas tabelas diferentes.
    /// </summary>
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }

    private StockMovement()
    {
    }

    private StockMovement(
        Guid productId,
        StockMovementType type,
        int quantity,
        int previousStock,
        int newStock,
        string reason,
        Guid userId,
        string? referenceType,
        Guid? referenceId)
    {
        if (quantity <= 0)
        {
            throw new QuantidadeInvalidaException("A quantidade da movimentação deve ser maior que zero.");
        }

        if (type == StockMovementType.Ajuste && string.IsNullOrWhiteSpace(reason))
        {
            throw new MotivoObrigatorioException();
        }

        ProductId = productId;
        Type = type;
        Quantity = quantity;
        PreviousStock = previousStock;
        NewStock = newStock;
        Reason = string.IsNullOrWhiteSpace(reason) ? type.ToString() : reason.Trim();
        UserId = userId;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
    }

    /// <summary>
    /// Cria uma movimentação de ENTRADA. Use em conjunto com
    /// <see cref="Product.DarEntradaEmEstoque"/>: primeiro chama o método
    /// do produto, depois usa o (estoqueAnterior, estoqueNovo) retornado
    /// para criar este registro.
    /// </summary>
    public static StockMovement CriarEntrada(
        Guid productId, int quantidade, int estoqueAnterior, int estoqueNovo,
        string motivo, Guid userId, string? referenceType = null, Guid? referenceId = null)
        => new(productId, StockMovementType.Entrada, quantidade, estoqueAnterior, estoqueNovo,
               motivo, userId, referenceType, referenceId);

    public static StockMovement CriarSaida(
        Guid productId, int quantidade, int estoqueAnterior, int estoqueNovo,
        string motivo, Guid userId, string? referenceType = null, Guid? referenceId = null)
        => new(productId, StockMovementType.Saida, quantidade, estoqueAnterior, estoqueNovo,
               motivo, userId, referenceType, referenceId);

    public static StockMovement CriarAjuste(
        Guid productId, int quantidade, int estoqueAnterior, int estoqueNovo,
        string motivo, Guid userId, string? referenceType = null, Guid? referenceId = null)
        => new(productId, StockMovementType.Ajuste, quantidade, estoqueAnterior, estoqueNovo,
               motivo, userId, referenceType, referenceId);
}
