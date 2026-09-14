namespace AdegaDoRatao.Domain.Enums;

/// <summary>
/// Status do ciclo de vida de uma compra (RN15/RN16/RN17).
/// </summary>
public enum PurchaseStatus
{
    /// <summary>Compra registrada, mas ainda não confirmada — não afeta estoque nem financeiro.</summary>
    Pendente = 1,

    /// <summary>Compra confirmada/recebida — dispara entrada de estoque e lançamento financeiro.</summary>
    Recebida = 2,

    /// <summary>Compra cancelada — se já estava recebida, estorna estoque e financeiro (quando possível).</summary>
    Cancelada = 3
}
