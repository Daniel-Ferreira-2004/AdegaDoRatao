using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Enums;
using AdegaDoRatao.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace AdegaDoRatao.UnitTests.Domain;

/// <summary>
/// Testes das regras de domínio do lançamento financeiro: RN24 (dados
/// obrigatórios), RN25 (origem automática) e estorno.
/// </summary>
public class FinancialTransactionTests
{
    // ----------------------------------------------------------------
    // Criação manual (RN24/RN26)
    // ----------------------------------------------------------------

    [Fact]
    public void CriarManual_ComDadosValidos_DeveCriarLancamentoPago()
    {
        var transaction = FinancialTransaction.CriarManual(
            "Conta de luz", FinancialTransactionType.Saida, Guid.NewGuid(),
            250.00m, DateTime.UtcNow, null, Guid.NewGuid(), null);

        transaction.Description.Should().Be("Conta de luz");
        transaction.Type.Should().Be(FinancialTransactionType.Saida);
        transaction.Amount.Should().Be(250.00m);
        transaction.Status.Should().Be(FinancialTransactionStatus.Pago);
        transaction.ReferenceType.Should().BeNull(); // manual não tem origem
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void CriarManual_ComValorInvalido_DeveFalhar(decimal valor)
    {
        // RN24: valor deve ser > 0
        var act = () => FinancialTransaction.CriarManual(
            "Teste", FinancialTransactionType.Saida, Guid.NewGuid(),
            valor, DateTime.UtcNow, null, Guid.NewGuid(), null);

        act.Should().Throw<ValorInvalidoException>();
    }

    [Fact]
    public void CriarManual_SemDescricao_DeveFalhar()
    {
        var act = () => FinancialTransaction.CriarManual(
            "", FinancialTransactionType.Saida, Guid.NewGuid(),
            100m, DateTime.UtcNow, null, Guid.NewGuid(), null);

        act.Should().Throw<ArgumentException>();
    }

    // ----------------------------------------------------------------
    // Lançamentos automáticos (RN25)
    // ----------------------------------------------------------------

    [Fact]
    public void CriarDeVenda_DeveGerarEntradaComReferencia()
    {
        var saleId = Guid.NewGuid();

        var transaction = FinancialTransaction.CriarDeVenda(
            saleId, 150.00m, DateTime.UtcNow, Guid.NewGuid(), null, Guid.NewGuid());

        transaction.Type.Should().Be(FinancialTransactionType.Entrada);
        transaction.ReferenceType.Should().Be("Sale");
        transaction.ReferenceId.Should().Be(saleId);
    }

    [Fact]
    public void CriarDeCompra_DeveGerarSaidaComReferencia()
    {
        var purchaseId = Guid.NewGuid();

        var transaction = FinancialTransaction.CriarDeCompra(
            purchaseId, 500.00m, DateTime.UtcNow, Guid.NewGuid(), null, Guid.NewGuid());

        transaction.Type.Should().Be(FinancialTransactionType.Saida);
        transaction.ReferenceType.Should().Be("Purchase");
        transaction.ReferenceId.Should().Be(purchaseId);
    }

    [Fact]
    public void CriarEstornoDeVenda_DeveGerarSaida()
    {
        var transaction = FinancialTransaction.CriarEstornoDeVenda(
            Guid.NewGuid(), 150.00m, DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid());

        transaction.Type.Should().Be(FinancialTransactionType.Saida);
        transaction.ReferenceType.Should().Be("Sale");
    }

    // ----------------------------------------------------------------
    // Estorno
    // ----------------------------------------------------------------

    [Fact]
    public void Estornar_LancamentoPago_DeveMudarStatusParaEstornado()
    {
        var transaction = FinancialTransaction.CriarManual(
            "Despesa", FinancialTransactionType.Saida, Guid.NewGuid(),
            100m, DateTime.UtcNow, null, Guid.NewGuid(), null);

        transaction.Estornar();

        transaction.Status.Should().Be(FinancialTransactionStatus.Estornado);
        transaction.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Estornar_LancamentoJaEstornado_DeveFalhar()
    {
        var transaction = FinancialTransaction.CriarManual(
            "Despesa", FinancialTransactionType.Saida, Guid.NewGuid(),
            100m, DateTime.UtcNow, null, Guid.NewGuid(), null);
        transaction.Estornar();

        var act = () => transaction.Estornar();

        act.Should().Throw<TransicaoDeStatusInvalidaException>();
    }
}
