namespace AdegaDoRatao.Domain.Enums;

/// <summary>
/// Tipo de um lançamento financeiro (RN24): dinheiro entrando ou saindo do caixa.
/// </summary>
public enum FinancialTransactionType
{
    Entrada = 1,
    Saida = 2
}

/// <summary>
/// Status de um lançamento financeiro.
/// </summary>
public enum FinancialTransactionStatus
{
    /// <summary>Lançamento registrado, aguardando confirmação/pagamento.</summary>
    Pendente = 1,

    /// <summary>Lançamento confirmado (dinheiro efetivamente recebido/pago).</summary>
    Pago = 2,

    /// <summary>Lançamento estornado (ex.: por cancelamento de venda/compra que o originou).</summary>
    Estornado = 3
}
