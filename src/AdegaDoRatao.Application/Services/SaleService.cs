using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Enums;
using AdegaDoRatao.Domain.Exceptions;
using AdegaDoRatao.Domain.Interfaces;

namespace AdegaDoRatao.Application.Services;

/// <summary>
/// Casos de uso de venda no balcão (RN18–RN22).
///
/// Por que a venda já nasce como CONCLUIDA? Diferente da compra (que fica
/// pendente até o caminhão chegar), no PDV o ato de registrar a venda é o
/// mesmo de baixar estoque e entrar dinheiro no caixa. Tudo isso precisa
/// acontecer na mesma transação: se a baixa de estoque falhar, o lançamento
/// financeiro também não pode ser gravado.
/// </summary>
public sealed class SaleService : ISaleService
{
    private const string CategoriaFinanceiraVendas = "Vendas";

    private readonly ISaleRepository _sales;
    private readonly IProductRepository _products;
    private readonly IStockMovementRepository _movements;
    private readonly IFinancialTransactionRepository _transactions;
    private readonly IFinancialCategoryRepository _categories;
    private readonly IPaymentMethodRepository _payments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public SaleService(
        ISaleRepository sales,
        IProductRepository products,
        IStockMovementRepository movements,
        IFinancialTransactionRepository transactions,
        IFinancialCategoryRepository categories,
        IPaymentMethodRepository payments,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IAuditService audit)
    {
        _sales = sales;
        _products = products;
        _movements = movements;
        _transactions = transactions;
        _categories = categories;
        _payments = payments;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<SaleResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => ToDetail(await ObterObrigatorioAsync(id, cancellationToken));

    public async Task<PagedResult<SaleListItemResponse>> SearchAsync(SaleQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var items = await _sales.PesquisarAsync(query.From, query.To, query.Status, page, pageSize, cancellationToken);
        var total = await _sales.ContarAsync(query.From, query.To, query.Status, cancellationToken);
        return new PagedResult<SaleListItemResponse>(items.Select(ToListItem).ToArray(), page, pageSize, total);
    }

    public async Task<SaleResponse> CreateAsync(CreateSaleRequest request, CancellationToken cancellationToken = default)
    {
        var userId = UsuarioAtual();
        var payment = await _payments.ObterPorIdAsync(request.PaymentMethodId, cancellationToken)
            ?? throw new UseCaseException("Forma de pagamento não encontrada.");
        if (!payment.IsActive)
            throw new UseCaseException("A forma de pagamento informada está inativa.");

        var products = await CarregarProdutosDaVendaAsync(request, cancellationToken);
        var salesCategory = await ObterCategoriaVendasAsync(cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // O número sequencial é lido dentro da transação para reduzir
            // (não eliminar) a chance de dois caixas gerarem o mesmo cupom.
            var sale = new Sale(
                await _sales.ObterProximoNumeroDeVendaAsync(cancellationToken),
                DateTime.UtcNow,
                userId,
                request.Discount,
                request.PaymentMethodId);

            foreach (var item in request.Items)
            {
                var product = products[item.ProductId];
                // RN18: o preço vigente do cadastro é congelado no item.
                sale.AdicionarItem(new SaleItem(product.Id, item.Quantity, product.SalePrice));
            }

            if (sale.Total <= 0)
                throw new UseCaseException("O total da venda deve ser maior que zero. Confira desconto e itens.");

            await _sales.AdicionarAsync(sale, cancellationToken);

            foreach (var item in sale.Items)
            {
                var product = products[item.ProductId];
                // RN19/RN11: a entidade Product recusa estoque negativo, salvo permissão explícita.
                var stock = product.DarSaidaEmEstoque(item.Quantity);
                _products.Atualizar(product);
                await _movements.AdicionarAsync(
                    StockMovement.CriarSaida(
                        product.Id,
                        item.Quantity,
                        stock.estoqueAnterior,
                        stock.estoqueNovo,
                        $"Venda #{sale.SaleNumber}",
                        userId,
                        "Sale",
                        sale.Id),
                    cancellationToken);
            }

            // RN20: entrada financeira automática, nunca editável depois.
            await _transactions.AdicionarAsync(
                FinancialTransaction.CriarDeVenda(sale.Id, sale.Total, sale.Date, salesCategory.Id, sale.PaymentMethodId, userId),
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            await _audit.RegisterAsync("SALE", nameof(Sale), sale.Id.ToString(), null, new { sale.SaleNumber, sale.Total }, cancellationToken);
            return ToDetail(sale);
        }
        catch (DomainException ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw new UseCaseException(ex.Message);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = UsuarioAtual();
        var sale = await ObterObrigatorioAsync(id, cancellationToken);
        if (sale.Status == SaleStatus.Cancelada)
            throw new UseCaseException("Esta venda já está cancelada.");

        var salesCategory = await ObterCategoriaVendasAsync(cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // RN21: a venda original permanece no histórico. O estorno gera
            // movimento de AJUSTE e um novo lançamento financeiro de SAÍDA.
            foreach (var item in sale.Items)
            {
                var product = await _products.ObterPorIdAsync(item.ProductId, cancellationToken)
                    ?? throw new UseCaseException("Produto da venda não encontrado para estorno.");
                var stock = product.DarEntradaEmEstoque(item.Quantity);
                _products.Atualizar(product);
                await _movements.AdicionarAsync(
                    StockMovement.CriarAjuste(
                        product.Id,
                        item.Quantity,
                        stock.estoqueAnterior,
                        stock.estoqueNovo,
                        $"Estorno de venda #{sale.SaleNumber}",
                        userId,
                        "Sale",
                        sale.Id),
                    cancellationToken);
            }

            sale.Cancelar();
            _sales.Atualizar(sale);
            await _transactions.AdicionarAsync(
                FinancialTransaction.CriarEstornoDeVenda(sale.Id, sale.Total, DateTime.UtcNow, salesCategory.Id, userId),
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (DomainException ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw new UseCaseException(ex.Message);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        await _audit.RegisterAsync("SALE_CANCEL", nameof(Sale), id.ToString(), null, new { sale.SaleNumber }, cancellationToken);
    }

    private Guid UsuarioAtual()
        => _currentUser.UserId ?? throw new UseCaseException("Usuário autenticado não identificado.");

    private async Task<Sale> ObterObrigatorioAsync(Guid id, CancellationToken cancellationToken)
        => await _sales.ObterPorIdAsync(id, cancellationToken) ?? throw new UseCaseException("Venda não encontrada.");

    private async Task<Dictionary<Guid, Product>> CarregarProdutosDaVendaAsync(CreateSaleRequest request, CancellationToken cancellationToken)
    {
        var products = new Dictionary<Guid, Product>();
        foreach (var item in request.Items)
        {
            var product = await _products.ObterPorIdAsync(item.ProductId, cancellationToken)
                ?? throw new UseCaseException("Produto da venda não encontrado.");
            if (!product.IsActive)
                throw new UseCaseException($"O produto '{product.Name}' está inativo e não pode ser vendido.");
            products[product.Id] = product;
        }

        return products;
    }

    private async Task<FinancialCategory> ObterCategoriaVendasAsync(CancellationToken cancellationToken)
    {
        var category = (await _categories.ListarTodosAsync(cancellationToken))
            .FirstOrDefault(c => c.IsActive && c.Name.Equals(CategoriaFinanceiraVendas, StringComparison.OrdinalIgnoreCase));
        return category ?? throw new UseCaseException(
            "A categoria financeira padrão 'Vendas' não está configurada. Cadastre-a antes de registrar vendas.");
    }

    private static SaleResponse ToDetail(Sale s) => new(
        s.Id, s.SaleNumber, s.Date, s.Discount, s.Total, s.PaymentMethodId, s.Status,
        s.Items.Select(i => new SaleItemResponse(i.ProductId, i.Quantity, i.UnitPrice, i.Subtotal)).ToArray());

    private static SaleListItemResponse ToListItem(Sale s) => new(
        s.Id, s.SaleNumber, s.Date, s.Discount, s.Total, s.PaymentMethodId, s.Status, s.Items.Count);
}
