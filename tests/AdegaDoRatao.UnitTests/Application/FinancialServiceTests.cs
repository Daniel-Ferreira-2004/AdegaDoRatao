using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Application.Services;
using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Enums;
using AdegaDoRatao.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace AdegaDoRatao.UnitTests.Application;

/// <summary>
/// Testes dos casos de uso financeiros: lançamentos manuais (RN24/RN26),
/// estorno (RN25) e fluxo de caixa (RN27).
/// </summary>
public class FinancialServiceTests
{
    private readonly Mock<IFinancialCategoryRepository> _categories = new();
    private readonly Mock<IFinancialTransactionRepository> _transactions = new();
    private readonly Mock<IPaymentMethodRepository> _paymentMethods = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditService> _audit = new();

    private FinancialService CriarServico()
        => new(_categories.Object, _transactions.Object, _paymentMethods.Object,
               _unitOfWork.Object, _currentUser.Object, _audit.Object);

    private void ConfigurarUsuarioAutenticado()
        => _currentUser.SetupGet(x => x.UserId).Returns(Guid.NewGuid());

    // ----------------------------------------------------------------
    // Lançamentos manuais (RN24/RN26)
    // ----------------------------------------------------------------

    [Fact]
    public async Task CreateTransactionAsync_ComDadosValidos_DevePersistir()
    {
        ConfigurarUsuarioAutenticado();
        var category = new FinancialCategory("Despesas", FinancialTransactionType.Saida);
        _categories.Setup(x => x.ObterPorIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        var service = CriarServico();
        var request = new CreateFinancialTransactionRequest(
            "Conta de luz", FinancialTransactionType.Saida, category.Id,
            250.00m, DateTime.UtcNow, null, null);

        var response = await service.CreateTransactionAsync(request);

        response.Description.Should().Be("Conta de luz");
        response.Amount.Should().Be(250.00m);
        _transactions.Verify(x => x.AdicionarAsync(It.IsAny<FinancialTransaction>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTransactionAsync_ComCategoriaInativa_DeveFalhar()
    {
        ConfigurarUsuarioAutenticado();
        var category = new FinancialCategory("Despesas", FinancialTransactionType.Saida);
        category.Desativar();
        _categories.Setup(x => x.ObterPorIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        var service = CriarServico();
        var request = new CreateFinancialTransactionRequest(
            "Conta de luz", FinancialTransactionType.Saida, category.Id,
            250.00m, DateTime.UtcNow, null, null);

        var act = () => service.CreateTransactionAsync(request);

        await act.Should().ThrowAsync<UseCaseException>().WithMessage("*inativa*");
    }

    [Fact]
    public async Task CreateTransactionAsync_SemUsuarioAutenticado_DeveFalhar()
    {
        _currentUser.SetupGet(x => x.UserId).Returns((Guid?)null);
        var service = CriarServico();
        var request = new CreateFinancialTransactionRequest(
            "Teste", FinancialTransactionType.Saida, Guid.NewGuid(),
            100m, DateTime.UtcNow, null, null);

        var act = () => service.CreateTransactionAsync(request);

        await act.Should().ThrowAsync<UseCaseException>();
    }

    // ----------------------------------------------------------------
    // Estorno (RN25)
    // ----------------------------------------------------------------

    [Fact]
    public async Task EstornarAsync_LancamentoManual_DeveEstornar()
    {
        var transaction = FinancialTransaction.CriarManual(
            "Despesa", FinancialTransactionType.Saida, Guid.NewGuid(),
            100m, DateTime.UtcNow, null, Guid.NewGuid(), null);
        _transactions.Setup(x => x.ObterPorIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        var service = CriarServico();

        await service.EstornarAsync(transaction.Id);

        transaction.Status.Should().Be(FinancialTransactionStatus.Estornado);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EstornarAsync_LancamentoAutomatico_DeveFalhar()
    {
        // RN25: lançamentos de venda/compra só são estornados pelo cancelamento da origem
        var transaction = FinancialTransaction.CriarDeVenda(
            Guid.NewGuid(), 150m, DateTime.UtcNow, Guid.NewGuid(), null, Guid.NewGuid());
        _transactions.Setup(x => x.ObterPorIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        var service = CriarServico();

        var act = () => service.EstornarAsync(transaction.Id);

        await act.Should().ThrowAsync<UseCaseException>().WithMessage("*automaticamente*");
    }

    // ----------------------------------------------------------------
    // Fluxo de caixa (RN27)
    // ----------------------------------------------------------------

    [Fact]
    public async Task GetCashFlowAsync_DeveCalcularSaldoComoEntradasMenosSaidas()
    {
        var from = new DateTime(2026, 9, 1);
        var to = new DateTime(2026, 9, 30);
        _transactions.Setup(x => x.SomarPorTipoEPeriodoAsync(
                FinancialTransactionType.Entrada, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10000m);
        _transactions.Setup(x => x.SomarPorTipoEPeriodoAsync(
                FinancialTransactionType.Saida, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3500m);
        var service = CriarServico();

        var result = await service.GetCashFlowAsync(from, to);

        result.TotalEntradas.Should().Be(10000m);
        result.TotalSaidas.Should().Be(3500m);
        result.Saldo.Should().Be(6500m);
    }

    [Fact]
    public async Task GetCashFlowAsync_PeriodoSemMovimento_DeveRetornarZeros()
    {
        var from = new DateTime(2026, 9, 1);
        var to = new DateTime(2026, 9, 30);
        _transactions.Setup(x => x.SomarPorTipoEPeriodoAsync(
                It.IsAny<FinancialTransactionType>(), from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);
        var service = CriarServico();

        var result = await service.GetCashFlowAsync(from, to);

        result.Saldo.Should().Be(0m);
    }
}
