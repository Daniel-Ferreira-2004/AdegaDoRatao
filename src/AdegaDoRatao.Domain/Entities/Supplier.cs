using AdegaDoRatao.Domain.Common;

namespace AdegaDoRatao.Domain.Entities;

/// <summary>
/// Fornecedor de produtos para a adega.
/// </summary>
public class Supplier : BaseAuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Document { get; private set; } // CNPJ ou CPF
    public string? Phone { get; private set; }
    public string? Email { get; private set; }

    private Supplier()
    {
    }

    public Supplier(string name, string? document, string? phone, string? email)
    {
        SetName(name);
        Document = string.IsNullOrWhiteSpace(document) ? null : document.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
    }

    public void AtualizarDados(string name, string? document, string? phone, string? email)
    {
        SetName(name);
        Document = string.IsNullOrWhiteSpace(document) ? null : document.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        MarcarComoAtualizada();
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("O nome do fornecedor é obrigatório.", nameof(name));
        }

        Name = name.Trim();
    }
}
