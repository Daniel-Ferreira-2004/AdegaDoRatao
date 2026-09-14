using AdegaDoRatao.Domain.Common;

namespace AdegaDoRatao.Domain.Entities;

/// <summary>
/// Categoria de produto (ex.: Cervejas, Destilados, Vinhos).
/// Ver RN07/RN08 em docs/03-REGRAS-DE-NEGOCIO.md.
/// </summary>
public class Category : BaseAuditableEntity
{
    public string Name { get; private set; } = string.Empty;

    // Construtor privado exigido pelo Entity Framework Core (materialização
    // via reflexão), para não expormos um construtor público sem validação.
    private Category()
    {
    }

    public Category(string name)
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
            throw new ArgumentException("O nome da categoria é obrigatório.", nameof(name));
        }

        Name = name.Trim();
    }
}
