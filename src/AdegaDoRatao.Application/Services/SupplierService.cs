using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Interfaces;

namespace AdegaDoRatao.Application.Services;

public sealed class SupplierService(ISupplierRepository suppliers, IUnitOfWork unitOfWork, IAuditService audit) : ISupplierService
{
    public async Task<SupplierResponse> CreateAsync(CreateSupplierRequest request, CancellationToken ct = default)
    { var supplier = new Supplier(request.Name, request.Document, request.Phone, request.Email); await suppliers.AdicionarAsync(supplier, ct); await unitOfWork.SaveChangesAsync(ct); await audit.RegisterAsync("CREATE", nameof(Supplier), supplier.Id.ToString(), null, ToResponse(supplier), ct); return ToResponse(supplier); }
    public async Task<SupplierResponse> GetByIdAsync(Guid id, CancellationToken ct = default) => ToResponse(await Required(id, ct));
    public async Task<IReadOnlyList<SupplierResponse>> ListAsync(CancellationToken ct = default) => (await suppliers.ListarTodosAsync(ct)).Select(ToResponse).ToArray();
    public async Task<SupplierResponse> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken ct = default)
    { var supplier = await Required(id, ct); supplier.AtualizarDados(request.Name, request.Document, request.Phone, request.Email); suppliers.Atualizar(supplier); await unitOfWork.SaveChangesAsync(ct); await audit.RegisterAsync("UPDATE", nameof(Supplier), id.ToString(), null, ToResponse(supplier), ct); return ToResponse(supplier); }
    public async Task SetActiveAsync(Guid id, bool active, CancellationToken ct = default)
    { var supplier = await Required(id, ct); if(active) supplier.Ativar(); else supplier.Desativar(); suppliers.Atualizar(supplier); await unitOfWork.SaveChangesAsync(ct); await audit.RegisterAsync(active?"ACTIVATE":"DEACTIVATE", nameof(Supplier), id.ToString(), null, new { supplier.IsActive }, ct); }
    private async Task<Supplier> Required(Guid id, CancellationToken ct) => await suppliers.ObterPorIdAsync(id,ct) ?? throw new UseCaseException("Fornecedor não encontrado.");
    private static SupplierResponse ToResponse(Supplier s) => new(s.Id,s.Name,s.Document,s.Phone,s.Email,s.IsActive,s.CreatedAt,s.UpdatedAt);
}
