namespace AdegaDoRatao.Domain.Enums;

/// <summary>
/// Tipo de uma movimentação de estoque (RN09/RN10 em 03-REGRAS-DE-NEGOCIO.md).
/// Toda alteração de quantidade em estoque acontece através de um destes
/// três tipos — nunca por atualização direta do campo de estoque do produto.
/// </summary>
public enum StockMovementType
{
    /// <summary>Entrada de mercadoria (ex.: recebimento de uma compra).</summary>
    Entrada = 1,

    /// <summary>Saída de mercadoria (ex.: baixa por uma venda).</summary>
    Saida = 2,

    /// <summary>Ajuste manual (correção de inventário, perda, quebra, estorno).</summary>
    Ajuste = 3
}
