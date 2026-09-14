using AdegaDoRatao.Domain.Common;

namespace AdegaDoRatao.Domain.Entities;

/// <summary>
/// Marca de um produto (ex.: Heineken, Coca-Cola, Jack Daniel's).
/// Cadastrada e gerenciada pelo usuário — nunca fixa no código (ver
/// item 3 "MARCAS" do documento original de requisitos).
/// </summary>
public class Brand : BaseAuditableEntity
{
    public string Name { get; private set; } = string.Empty;

    private Brand()
    {
    }

    public Brand(string name)
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
            throw new ArgumentException("O nome da marca é obrigatório.", nameof(name));
        }

        Name = name.Trim();
    }
}
