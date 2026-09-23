namespace AdegaDoRatao.Domain.Enums;

/// <summary>
/// Classificação do preço coletado numa rede. Preços de condições
/// diferentes NUNCA devem ser comparados diretamente entre si.
/// </summary>
public enum TipoPrecoColeta
{
    /// <summary>Preço unitário normal, sem condição.</summary>
    Normal,

    /// <summary>Preço promocional (ex.: oferta, clube/fidelidade, app).</summary>
    Promocional,

    /// <summary>Preço condicionado à quantidade (ex.: "a partir de 6 unid.").</summary>
    CondicionadoQuantidade,

    /// <summary>Preço por unidade de medida (kg, litro etc.).</summary>
    PorUnidadeMedida,

    /// <summary>Preço não confirmado — nunca tratar como definitivo.</summary>
    NaoConfirmado
}

/// <summary>
/// Nível de confiança do dado coletado, considerando correspondência do
/// produto (EAN vs. nome), confirmação da região e origem do preço.
/// </summary>
public enum NivelConfiancaColeta
{
    /// <summary>Produto confirmado por EAN + preço regionalizado confirmado.</summary>
    High,

    /// <summary>Produto confirmado por EAN ou preço regionalizado, mas não ambos.</summary>
    Medium,

    /// <summary>Produto identificado apenas por nome; região não confirmada.</summary>
    Low,

    /// <summary>Dado não confirmado — não usar como preço definitivo.</summary>
    Unverified
}
