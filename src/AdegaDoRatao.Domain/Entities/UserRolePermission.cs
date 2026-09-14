using AdegaDoRatao.Domain.Common;

namespace AdegaDoRatao.Domain.Entities;

/// <summary>
/// Permissão granular do sistema (ex.: "products.write", "financial.read").
/// Cadastrada como dado (não como enum no código) para permitir evoluir o
/// modelo de permissões sem precisar de deploy — ver docs/06-SEGURANCA.md.
/// </summary>
public class Permission : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    private Permission()
    {
    }

    public Permission(string code, string description)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("O código da permissão é obrigatório.", nameof(code));
        }

        Code = code.Trim().ToLowerInvariant();
        Description = description?.Trim() ?? string.Empty;
    }
}

/// <summary>
/// Associação N:N entre Role e Permission (tabela de junção explícita,
/// em vez de uma coleção "mágica" do EF, para deixar claro que é uma
/// entidade de verdade — poderia futuramente ganhar campos próprios,
/// como "concedida por" / "concedida em").
/// </summary>
public class RolePermission
{
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }

    private RolePermission()
    {
    }

    public RolePermission(Guid roleId, Guid permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }
}

/// <summary>
/// Perfil de usuário (ADMIN, GERENTE, OPERADOR — RN30). O conjunto de
/// permissões de cada Role é dado (RolePermission), não código-fonte,
/// permitindo criar novos perfis futuramente via cadastro.
/// </summary>
public class Role : BaseEntity
{
    private readonly List<RolePermission> _rolePermissions = new();

    public string Name { get; private set; } = string.Empty;

    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    private Role()
    {
    }

    public Role(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("O nome do perfil é obrigatório.", nameof(name));
        }

        Name = name.Trim().ToUpperInvariant();
    }

    public void ConcederPermissao(Guid permissionId)
    {
        if (_rolePermissions.Any(rp => rp.PermissionId == permissionId))
        {
            return; // já concedida — operação idempotente
        }

        _rolePermissions.Add(new RolePermission(Id, permissionId));
    }

    public void RevogarPermissao(Guid permissionId)
    {
        _rolePermissions.RemoveAll(rp => rp.PermissionId == permissionId);
    }
}

/// <summary>
/// Usuário do sistema (RN30/RN31). A senha nunca é armazenada em texto
/// puro — apenas o hash, calculado pela Infrastructure (IPasswordHasher,
/// ver docs/06-SEGURANCA.md) e passado pronto para este construtor.
/// </summary>
public class User : BaseAuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public Guid RoleId { get; private set; }

    private User()
    {
    }

    public User(string name, string email, string passwordHash, Guid roleId)
    {
        SetName(name);
        SetEmail(email);

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("O hash de senha é obrigatório.", nameof(passwordHash));
        }

        PasswordHash = passwordHash;
        RoleId = roleId;
    }

    public void AtualizarDados(string name, string email)
    {
        SetName(name);
        SetEmail(email);
        MarcarComoAtualizada();
    }

    public void AlterarSenha(string novoHash)
    {
        if (string.IsNullOrWhiteSpace(novoHash))
        {
            throw new ArgumentException("O hash de senha é obrigatório.", nameof(novoHash));
        }

        PasswordHash = novoHash;
        MarcarComoAtualizada();
    }

    public void AlterarPerfil(Guid roleId)
    {
        RoleId = roleId;
        MarcarComoAtualizada();
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("O nome do usuário é obrigatório.", nameof(name));
        }

        Name = name.Trim();
    }

    private void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("O e-mail do usuário é obrigatório.", nameof(email));
        }

        Email = email.Trim().ToLowerInvariant();
    }
}
