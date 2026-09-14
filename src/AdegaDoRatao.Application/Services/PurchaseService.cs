using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Enums;
using AdegaDoRatao.Domain.Interfaces;

namespace AdegaDoRatao.Application.Services;

/// <summary>Casos de uso de compra; somente a confirmação afeta estoque e caixa (RN15/RN16).</summary>
public sealed class PurchaseService : IPurchaseService
{
    private readonly IPurchaseRepository _purchases;
    private readonly ISupplierRepository _suppliers;
    private readonly IProductRepository _products;
    private readonly IStockMovementRepository _movements;
    private readonly IFinancialTransactionRepository _transactions;
    private readonly IFinancialCategoryRepository _categories;
    private readonly IPaymentMethodRepository _payments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public PurchaseService(IPurchaseRepository purchases, ISupplierRepository suppliers, IProductRepository products,
        IStockMovementRepository movements, IFinancialTransactionRepository transactions, IFinancialCategoryRepository categories,
        IPaymentMethodRepository payments, IUnitOfWork unitOfWork, ICurrentUserService currentUser, IAuditService audit)
        => (_purchases, _suppliers, _products, _movements, _transactions, _categories, _payments, _unitOfWork, _currentUser, _audit) =
           (purchases, suppliers, products, movements, transactions, categories, payments, unitOfWork, currentUser, audit);

    public async Task<PurchaseResponse> CreateAsync(CreatePurchaseRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId ?? throw new UseCaseException("Usuário autenticado não identificado.");
        var supplier = await _suppliers.ObterPorIdAsync(request.SupplierId, cancellationToken) ?? throw new UseCaseException("Fornecedor não encontrado.");
        if (!supplier.IsActive) throw new UseCaseException("O fornecedor informado está inativo.");
        _ = await _payments.ObterPorIdAsync(request.PaymentMethodId, cancellationToken) ?? throw new UseCaseException("Forma de pagamento não encontrada.");
        var purchase = new Purchase(request.SupplierId, request.Date, request.Discount, request.Freight, request.PaymentMethodId, userId);
        foreach (var item in request.Items)
        {
            var product = await _products.ObterPorIdAsync(item.ProductId, cancellationToken) ?? throw new UseCaseException("Produto da compra não encontrado.");
            if (!product.IsActive) throw new UseCaseException($"O produto '{product.Name}' está inativo e não pode receber compra.");
            purchase.AdicionarItem(new PurchaseItem(item.ProductId, item.Quantity, item.UnitCost));
        }
        if (purchase.Total <= 0) throw new UseCaseException("O total da compra deve ser maior que zero.");
        await _purchases.AdicionarAsync(purchase, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.RegisterAsync("PURCHASE_CREATE", nameof(Purchase), purchase.Id.ToString(), null, new { purchase.Total }, cancellationToken);
        return ToResponse(purchase);
    }

    public async Task ConfirmAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId ?? throw new UseCaseException("Usuário autenticado não identificado.");
        var purchase = await GetRequiredAsync(id, cancellationToken);
        var purchasesCategory = await GetFinancialCategoryAsync("COMPRAS", cancellationToken);
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            purchase.ConfirmarRecebimento();
            _purchases.Atualizar(purchase);
            foreach (var item in purchase.Items)
            {
                var product = await _products.ObterPorIdAsync(item.ProductId, cancellationToken) ?? throw new UseCaseException("Produto da compra não encontrado.");
                if (!product.IsActive) throw new UseCaseException($"O produto '{product.Name}' está inativo e não pode receber compra.");
                var stock = product.DarEntradaEmEstoque(item.Quantity);
                _products.Atualizar(product);
                await _movements.AdicionarAsync(StockMovement.CriarEntrada(product.Id, item.Quantity, stock.estoqueAnterior, stock.estoqueNovo,
                    $"Compra #{purchase.Id}", userId, "Purchase", purchase.Id), cancellationToken);
            }
            await _transactions.AdicionarAsync(FinancialTransaction.CriarDeCompra(purchase.Id, purchase.Total, DateTime.UtcNow,
                purchasesCategory.Id, purchase.PaymentMethodId, userId), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch { await _unitOfWork.RollbackTransactionAsync(cancellationToken); throw; }
        await _audit.RegisterAsync("PURCHASE_CONFIRM", nameof(Purchase), id.ToString(), null, new { purchase.Total }, cancellationToken);
    }

    public async Task CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId ?? throw new UseCaseException("Usuário autenticado não identificado.");
        var purchase = await GetRequiredAsync(id, cancellationToken);
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (purchase.Status == PurchaseStatus.Recebida)
            {
                foreach (var item in purchase.Items)
                {
                    var product = await _products.ObterPorIdAsync(item.ProductId, cancellationToken) ?? throw new UseCaseException("Produto da compra não encontrado.");
                    if (product.CurrentStock < item.Quantity)
                        throw new UseCaseException("Não é possível estornar esta compra: parte do estoque já foi usada. Faça um ajuste manual.");
                    var stock = product.DarSaidaEmEstoque(item.Quantity);
                    _products.Atualizar(product);
                    await _movements.AdicionarAsync(StockMovement.CriarAjuste(product.Id, item.Quantity, stock.estoqueAnterior, stock.estoqueNovo,
                        $"Estorno de compra #{purchase.Id}", userId, "Purchase", purchase.Id), cancellationToken);
                }
                var category = await GetFinancialCategoryAsync("COMPRAS", cancellationToken);
                await _transactions.AdicionarAsync(FinancialTransaction.CriarEstornoDeCompra(purchase.Id, purchase.Total, DateTime.UtcNow, category.Id, userId), cancellationToken);
            }
            purchase.Cancelar();
            _purchases.Atualizar(purchase);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch { await _unitOfWork.RollbackTransactionAsync(cancellationToken); throw; }
        await _audit.RegisterAsync("PURCHASE_CANCEL", nameof(Purchase), id.ToString(), null, new { purchase.Total }, cancellationToken);
    }

    private async Task<Purchase> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
        => await _purchases.ObterPorIdAsync(id, cancellationToken) ?? throw new UseCaseException("Compra não encontrada.");
    private async Task<FinancialCategory> GetFinancialCategoryAsync(string name, CancellationToken cancellationToken)
        => (await _categories.ListarTodosAsync(cancellationToken)).FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
           ?? throw new UseCaseException($"A categoria financeira padrão '{name}' não está configurada.");
    private static PurchaseResponse ToResponse(Purchase p) => new(p.Id, p.SupplierId, p.Date, p.Discount, p.Freight, p.Total, p.Status);
}
