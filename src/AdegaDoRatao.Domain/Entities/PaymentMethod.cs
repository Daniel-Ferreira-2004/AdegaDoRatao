using AdegaDoRatao.Domain.Common;

namespace AdegaDoRatao.Domain.Entities;

/// <summary>
/// Forma de pagamento (Dinheiro, PIX, Cartão de Débito, Cartão de Crédito,
/// Outros). Modelada como tabela de referência (não enum fixo no código)
/// para permitir cadastrar novas formas sem alterar o código-fonte — ver
/// decisão registrada em docs/04-BANCO-DE-DADOS.md, seção 1.
/// </summary>
public class PaymentMethod : BaseAuditableEntity
{
    public string Name { get; private set; } = string.Empty;

    private PaymentMethod()
    {
    }

    public PaymentMethod(string name)
    {
        SetName(name);
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
            throw new ArgumentException("O nome da forma de pagamento é obrigatório.", nameof(name));
        }

        Name = name.Trim();
    }
}
