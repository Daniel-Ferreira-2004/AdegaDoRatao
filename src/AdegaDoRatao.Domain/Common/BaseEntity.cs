namespace AdegaDoRatao.Domain.Common;

/// <summary>
/// Classe base para todas as entidades do domínio.
///
/// Centraliza o que é comum a praticamente toda entidade do sistema:
/// - Id único (Guid, gerado no próprio domínio, não pelo banco);
/// - data de criação;
/// - data da última atualização.
///
/// Por que Guid e não int/long? Porque geramos o Id na camada de Domínio
/// (no construtor da entidade), sem depender do banco de dados para isso.
/// Isso facilita testes unitários (não precisamos persistir nada para ter
/// um Id válido) e evita expor Ids sequenciais previsíveis na API.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; }

    public DateTime CreatedAt { get; protected set; }

    public DateTime? UpdatedAt { get; protected set; }

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Construtor usado pelo Entity Framework Core ao materializar a entidade
    /// a partir do banco (o EF precisa conseguir criar o objeto sem passar
    /// pelas regras de negócio do construtor "de domínio").
    /// </summary>
    protected BaseEntity(Guid id, DateTime createdAt, DateTime? updatedAt)
    {
        Id = id;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// Marca a entidade como atualizada agora. Chamado internamente pelos
    /// métodos de negócio de cada entidade sempre que algum estado muda.
    /// </summary>
    protected void MarcarComoAtualizada()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Classe base para entidades que suportam exclusão lógica (soft delete),
/// como Product, Category, Brand, Supplier e User (ver RN36 em
/// docs/03-REGRAS-DE-NEGOCIO.md: exclusões são sempre lógicas para
/// entidades referenciadas por histórico).
/// </summary>
public abstract class BaseAuditableEntity : BaseEntity
{
    public bool IsActive { get; protected set; } = true;

    protected BaseAuditableEntity() : base()
    {
    }

    protected BaseAuditableEntity(Guid id, DateTime createdAt, DateTime? updatedAt, bool isActive)
        : base(id, createdAt, updatedAt)
    {
        IsActive = isActive;
    }

    public virtual void Ativar()
    {
        IsActive = true;
        MarcarComoAtualizada();
    }

    public virtual void Desativar()
    {
        IsActive = false;
        MarcarComoAtualizada();
    }
}
