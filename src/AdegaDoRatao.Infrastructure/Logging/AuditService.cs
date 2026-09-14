using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Interfaces;
using System.Text.Json;

namespace AdegaDoRatao.Infrastructure.Logging;
public sealed class AuditService(IAuditLogRepository logs, ICurrentUserService currentUser, IUnitOfWork unitOfWork) : IAuditService
{ public async Task RegisterAsync(string action,string entityName,string entityId,object? oldValues,object? newValues,CancellationToken ct=default) { await logs.AdicionarAsync(new AuditLog(currentUser.UserId,action,entityName,entityId,oldValues is null?null:JsonSerializer.Serialize(oldValues),newValues is null?null:JsonSerializer.Serialize(newValues)),ct); await unitOfWork.SaveChangesAsync(ct); } }
