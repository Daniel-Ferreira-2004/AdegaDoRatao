namespace AdegaDoRatao.Application.DTOs;

public sealed record NamedEntityRequest(string Name);
public sealed record NamedEntityResponse(Guid Id, string Name, bool IsActive, DateTime CreatedAt, DateTime? UpdatedAt);
