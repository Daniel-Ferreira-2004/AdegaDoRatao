namespace AdegaDoRatao.Application.DTOs;
public sealed record LoginRequest(string Email, string Password);
public sealed record LoginResponse(string AccessToken, DateTime ExpiresAt, Guid UserId, string Name, string Role, IReadOnlyCollection<string> Permissions);
