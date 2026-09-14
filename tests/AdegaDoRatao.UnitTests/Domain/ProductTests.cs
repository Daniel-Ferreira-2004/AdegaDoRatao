using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace AdegaDoRatao.UnitTests.Domain;

/// <summary>
/// Testes das regras de domínio do produto: RN02 (preços), RN03 (produto
/// inativo), RN06 (limites de estoque), RN09/RN11 (movimentação de estoque)
/// e RN29 (estoque baixo/zerado).
/// </summary>
public class ProductTests
{
    private static Product CriarProdutoValido(bool allowNegativeStock = false) => new(
        name: "Cerveja Skol 350ml",
        description: null,
        sku: "SKOL-350",
        barcode: null,
        categoryId: Guid.NewGuid(),
        brandId: Guid.NewGuid(),
        unitOfMeasure: "UN",
        costPrice: 2.50m,
        salePrice: 5.00m,
        minStock: 10,
        maxStock: 100,
        allowNegativeStock: allowNegativeStock);

    // ----------------------------------------------------------------
    // Cadastro
    // ----------------------------------------------------------------

    [Fact]
    public void Construtor_ComDadosValidos_DeveCriarProdutoComEstoqueZero()
    {
        var product = CriarProdutoValido();

        product.Name.Should().Be("Cerveja Skol 350ml");
        product.Sku.Should().Be("SKOL-350");
        product.CurrentStock.Should().Be(0); // RN09: nasce zerado, entra por movimentação
        product.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Construtor_ComNomeVazio_DeveFalhar(string nome)
    {
        var act = () => new Product(nome, null, "SKU-1", null, Guid.NewGuid(), Guid.NewGuid(),
            "UN", 1m, 2m, 0, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Construtor_ComPrecoDeCustoNegativo_DeveFalhar()
    {
        // RN02
        var act = () => new Product("Produto", null, "SKU-1", null, Guid.NewGuid(), Guid.NewGuid(),
            "UN", -1m, 2m, 0, null);

        act.Should().Throw<ValorInvalidoException>();
    }

    [Fact]
    public void Construtor_ComPrecoDeVendaNegativo_DeveFalhar()
    {
        // RN02
        var act = () => new Product("Produto", null, "SKU-1", null, Guid.NewGuid(), Guid.NewGuid(),
            "UN", 1m, -2m, 0, null);

        act.Should().Throw<ValorInvalidoException>();
    }

    // ----------------------------------------------------------------
    // Estoque — entrada (RN09)
    // ----------------------------------------------------------------

    [Fact]
    public void DarEntradaEmEstoque_DeveAumentarEstoqueAtual()
    {
        var product = CriarProdutoValido();

        var (anterior, novo) = product.DarEntradaEmEstoque(50);

        anterior.Should().Be(0);
        novo.Should().Be(50);
        product.CurrentStock.Should().Be(50);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void DarEntradaEmEstoque_ComQuantidadeInvalida_DeveFalhar(int quantidade)
    {
        var product = CriarProdutoValido();

        var act = () => product.DarEntradaEmEstoque(quantidade);

        act.Should().Throw<QuantidadeInvalidaException>();
    }

    // ----------------------------------------------------------------
    // Estoque — saída (RN03/RN11)
    // ----------------------------------------------------------------

    [Fact]
    public void DarSaidaEmEstoque_ComEstoqueSuficiente_DeveReduzirEstoque()
    {
        var product = CriarProdutoValido();
        product.DarEntradaEmEstoque(50);

        var (anterior, novo) = product.DarSaidaEmEstoque(20);

        anterior.Should().Be(50);
        novo.Should().Be(30);
        product.CurrentStock.Should().Be(30);
    }

    [Fact]
    public void DarSaidaEmEstoque_SemEstoqueSuficiente_DeveFalhar()
    {
        // RN11: estoque não pode ficar negativo por padrão
        var product = CriarProdutoValido();
        product.DarEntradaEmEstoque(10);

        var act = () => product.DarSaidaEmEstoque(20);

        act.Should().Throw<EstoqueInsuficienteException>();
        product.CurrentStock.Should().Be(10); // estoque não foi alterado
    }

    [Fact]
    public void DarSaidaEmEstoque_ComEstoqueNegativoPermitido_DevePermitir()
    {
        // RN11: exceção à regra quando AllowNegativeStock = true
        var product = CriarProdutoValido(allowNegativeStock: true);
        product.DarEntradaEmEstoque(10);

        var (_, novo) = product.DarSaidaEmEstoque(20);

        novo.Should().Be(-10);
        product.CurrentStock.Should().Be(-10);
    }

    [Fact]
    public void DarSaidaEmEstoque_ComProdutoInativo_DeveFalhar()
    {
        // RN03
        var product = CriarProdutoValido();
        product.DarEntradaEmEstoque(50);
        product.Desativar();

        var act = () => product.DarSaidaEmEstoque(10);

        act.Should().Throw<ProdutoInativoException>();
    }

    // ----------------------------------------------------------------
    // Estoque — ajuste (RN12)
    // ----------------------------------------------------------------

    [Fact]
    public void AjustarEstoque_SemMotivo_DeveFalhar()
    {
        // RN12: ajuste exige motivo obrigatório
        var product = CriarProdutoValido();

        var act = () => product.AjustarEstoque(10, "");

        act.Should().Throw<MotivoObrigatorioException>();
    }

    [Fact]
    public void AjustarEstoque_ComMotivo_DeveDefinirEstoqueAbsoluto()
    {
        var product = CriarProdutoValido();
        product.DarEntradaEmEstoque(50);

        var (anterior, novo) = product.AjustarEstoque(35, "Contagem de inventário");

        anterior.Should().Be(50);
        novo.Should().Be(35);
    }

    // ----------------------------------------------------------------
    // Indicadores (RN29)
    // ----------------------------------------------------------------

    [Fact]
    public void SemEstoque_ComEstoqueZero_DeveRetornarTrue()
    {
        var product = CriarProdutoValido();

        product.SemEstoque.Should().BeTrue();
        product.EstoqueBaixo.Should().BeFalse();
    }

    [Fact]
    public void EstoqueBaixo_ComEstoqueAteMinimo_DeveRetornarTrue()
    {
        var product = CriarProdutoValido(); // minStock = 10
        product.DarEntradaEmEstoque(5);

        product.EstoqueBaixo.Should().BeTrue();
        product.SemEstoque.Should().BeFalse();
    }

    [Fact]
    public void EstoqueBaixo_ComEstoqueAcimaDoMinimo_DeveRetornarFalse()
    {
        var product = CriarProdutoValido(); // minStock = 10
        product.DarEntradaEmEstoque(50);

        product.EstoqueBaixo.Should().BeFalse();
    }

    // ----------------------------------------------------------------
    // Preços (RN02)
    // ----------------------------------------------------------------

    [Fact]
    public void AlterarPrecos_ComValoresValidos_DeveAtualizar()
    {
        var product = CriarProdutoValido();

        product.AlterarPrecos(3.00m, 6.00m);

        product.CostPrice.Should().Be(3.00m);
        product.SalePrice.Should().Be(6.00m);
        product.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void AlterarPrecos_ComValorNegativo_DeveFalhar()
    {
        var product = CriarProdutoValido();

        var act = () => product.AlterarPrecos(-1m, 6.00m);

        act.Should().Throw<ValorInvalidoException>();
    }
}
