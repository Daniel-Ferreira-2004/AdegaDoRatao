namespace AdegaDoRatao.Domain.Enums;

/// <summary>
/// Status do ciclo de vida de uma venda (RN20/RN21).
/// </summary>
public enum SaleStatus
{
    /// <summary>Venda finalizada — estoque já baixado e financeiro já lançado.</summary>
    Concluida = 1,

    /// <summary>Venda cancelada — estoque estornado e financeiro de estorno lançado.</summary>
    Cancelada = 2
}
