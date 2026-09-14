using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Interfaces;

namespace AdegaDoRatao.Application.Services;

/// <summary>
/// Formas de pagamento (Dinheiro, PIX, cartões). São cadastro, não enum,
/// para o caixa poder incluir uma nova forma sem alterar o código.
/// Expostas já na Etapa 10 porque a venda exige um PaymentMethodId.
/// </summary>
public sealed class PaymentMethodService(
    IPaymentMethodRepository repository,
    IUnitOfWork unitOfWork,
    IAuditService audit) : IPaymentMethodService
{
    public async Task<NamedEntityResponse> CreateAsync(NamedEntityRequest request, CancellationToken cancellationToken = default)
    {
        if (await repository.NomeJaExisteAsync(request.Name, null, cancellationToken))
            throw new UseCaseException("Já existe uma forma de pagamento com este nome.");

        var method = new PaymentMethod(request.Name);
        await repository.AdicionarAsync(method, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await audit.RegisterAsync("CREATE", nameof(PaymentMethod), method.Id.ToString(), null, ToResponse(method), cancellationToken);
        return ToResponse(method);
    }

    public async Task<IReadOnlyList<NamedEntityResponse>> ListAsync(CancellationToken cancellationToken = default)
        => (await repository.ListarTodosAsync(cancellationToken)).Select(ToResponse).ToArray();

    public async Task SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default)
    {
        var method = await repository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new UseCaseException("Forma de pagamento não encontrada.");
        if (active) method.Ativar(); else method.Desativar();
        repository.Atualizar(method);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await audit.RegisterAsync(active ? "ACTIVATE" : "DEACTIVATE", nameof(PaymentMethod), id.ToString(), null, new { method.IsActive }, cancellationToken);
    }

    private static NamedEntityResponse ToResponse(PaymentMethod m)
        => new(m.Id, m.Name, m.IsActive, m.CreatedAt, m.UpdatedAt);
}
