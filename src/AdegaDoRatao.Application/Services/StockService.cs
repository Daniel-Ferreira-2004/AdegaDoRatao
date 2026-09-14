using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Enums;
using AdegaDoRatao.Domain.Interfaces;

namespace AdegaDoRatao.Application.Services;

/// <summary>
/// Registra movimentos e altera o saldo do produto na mesma unidade de
/// trabalho. Este é o único caminho de aplicação para mudar o estoque (RN09).
/// Para AJUSTE, Quantity significa o saldo contado/apurado, e não uma variação.
/// </summary>
public sealed class StockService : IStockService
{
    private readonly IProductRepository _products;
    private readonly IStockMovementRepository _movements;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public StockService(IProductRepository products, IStockMovementRepository movements, IUnitOfWork unitOfWork,
        ICurrentUserService currentUser, IAuditService audit)
        => (_products, _movements, _unitOfWork, _currentUser, _audit) = (products, movements, unitOfWork, currentUser, audit);

    public async Task<StockMovementResponse> RegisterMovementAsync(RegisterStockMovementRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId ?? throw new UseCaseException("Usuário autenticado não identificado.");
        var product = await _products.ObterPorIdAsync(request.ProductId, cancellationToken)
            ?? throw new UseCaseException("Produto não encontrado.");

        (int previous, int current) stock;
        StockMovement movement;
        switch (request.Type)
        {
            case StockMovementType.Entrada:
                stock = product.DarEntradaEmEstoque(request.Quantity);
                movement = StockMovement.CriarEntrada(product.Id, request.Quantity, stock.previous, stock.current, request.Reason, userId);
                break;
            case StockMovementType.Saida:
                stock = product.DarSaidaEmEstoque(request.Quantity);
                movement = StockMovement.CriarSaida(product.Id, request.Quantity, stock.previous, stock.current, request.Reason, userId);
                break;
            case StockMovementType.Ajuste:
                stock = product.AjustarEstoque(request.Quantity, request.Reason);
                var difference = Math.Abs(stock.current - stock.previous);
                if (difference == 0) throw new UseCaseException("O estoque informado já é o saldo atual do produto.");
                movement = StockMovement.CriarAjuste(product.Id, difference, stock.previous, stock.current, request.Reason, userId);
                break;
            default:
                throw new UseCaseException("Tipo de movimentação inválido.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            _products.Atualizar(product);
            await _movements.AdicionarAsync(movement, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        await _audit.RegisterAsync("STOCK_MOVEMENT", nameof(Product), product.Id.ToString(),
            new { CurrentStock = stock.previous }, new { CurrentStock = stock.current, request.Type, movement.Id }, cancellationToken);
        return ToResponse(movement);
    }

    public async Task<IReadOnlyList<StockMovementResponse>> GetHistoryAsync(Guid productId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        _ = await _products.ObterPorIdAsync(productId, cancellationToken)
            ?? throw new UseCaseException("Produto não encontrado.");
        var movements = await _movements.ListarPorProdutoAsync(productId, from, to, cancellationToken);
        return movements.Select(ToResponse).ToArray();
    }

    private static StockMovementResponse ToResponse(StockMovement m) => new(m.Id, m.ProductId, m.Type, m.Quantity,
        m.PreviousStock, m.NewStock, m.Reason, m.UserId, m.CreatedAt, m.ReferenceType, m.ReferenceId);
}
