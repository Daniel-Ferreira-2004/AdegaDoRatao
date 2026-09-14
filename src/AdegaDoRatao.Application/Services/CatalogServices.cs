using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Interfaces;

namespace AdegaDoRatao.Application.Services;

/// <summary>Casos de uso de categorias, incluindo a unicidade de nome (RN07).</summary>
public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _audit;
    public CategoryService(ICategoryRepository repository, IUnitOfWork unitOfWork, IAuditService audit)
        => (_repository, _unitOfWork, _audit) = (repository, unitOfWork, audit);

    public async Task<NamedEntityResponse> CreateAsync(NamedEntityRequest request, CancellationToken cancellationToken = default)
    {
        if (await _repository.NomeJaExisteAsync(request.Name, null, cancellationToken))
            throw new UseCaseException("Já existe uma categoria com este nome.");
        var category = new Category(request.Name);
        await _repository.AdicionarAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.RegisterAsync("CREATE", nameof(Category), category.Id.ToString(), null, ToResponse(category), cancellationToken);
        return ToResponse(category);
    }
    public async Task<IReadOnlyList<NamedEntityResponse>> ListAsync(CancellationToken cancellationToken = default)
        => (await _repository.ListarTodosAsync(cancellationToken)).Select(ToResponse).ToArray();
    public async Task SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default)
    {
        var category = await _repository.ObterPorIdAsync(id, cancellationToken) ?? throw new UseCaseException("Categoria não encontrada.");
        if (active) category.Ativar(); else category.Desativar();
        _repository.Atualizar(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.RegisterAsync(active ? "ACTIVATE" : "DEACTIVATE", nameof(Category), id.ToString(), null, new { category.IsActive }, cancellationToken);
    }
    private static NamedEntityResponse ToResponse(Category c) => new(c.Id, c.Name, c.IsActive, c.CreatedAt, c.UpdatedAt);
}

/// <summary>Casos de uso de marcas, mantendo a mesma regra de unicidade das categorias.</summary>
public sealed class BrandService : IBrandService
{
    private readonly IBrandRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _audit;
    public BrandService(IBrandRepository repository, IUnitOfWork unitOfWork, IAuditService audit)
        => (_repository, _unitOfWork, _audit) = (repository, unitOfWork, audit);

    public async Task<NamedEntityResponse> CreateAsync(NamedEntityRequest request, CancellationToken cancellationToken = default)
    {
        if (await _repository.NomeJaExisteAsync(request.Name, null, cancellationToken))
            throw new UseCaseException("Já existe uma marca com este nome.");
        var brand = new Brand(request.Name);
        await _repository.AdicionarAsync(brand, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.RegisterAsync("CREATE", nameof(Brand), brand.Id.ToString(), null, ToResponse(brand), cancellationToken);
        return ToResponse(brand);
    }
    public async Task<IReadOnlyList<NamedEntityResponse>> ListAsync(CancellationToken cancellationToken = default)
        => (await _repository.ListarTodosAsync(cancellationToken)).Select(ToResponse).ToArray();
    public async Task SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default)
    {
        var brand = await _repository.ObterPorIdAsync(id, cancellationToken) ?? throw new UseCaseException("Marca não encontrada.");
        if (active) brand.Ativar(); else brand.Desativar();
        _repository.Atualizar(brand);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.RegisterAsync(active ? "ACTIVATE" : "DEACTIVATE", nameof(Brand), id.ToString(), null, new { brand.IsActive }, cancellationToken);
    }
    private static NamedEntityResponse ToResponse(Brand b) => new(b.Id, b.Name, b.IsActive, b.CreatedAt, b.UpdatedAt);
}
