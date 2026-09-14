using AdegaDoRatao.Domain.Entities;

namespace AdegaDoRatao.Domain.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<bool> NomeJaExisteAsync(string nome, Guid? ignorarCategoryId = null, CancellationToken cancellationToken = default);
}

public interface IBrandRepository : IRepository<Brand>
{
    Task<bool> NomeJaExisteAsync(string nome, Guid? ignorarBrandId = null, CancellationToken cancellationToken = default);
}
