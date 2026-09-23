using AdegaDoRatao.Domain.Enums;
using AdegaDoRatao.Infrastructure.ExternalServices.Coletores;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AdegaDoRatao.UnitTests.Infrastructure.Coletores;

/// <summary>
/// Testes do coletor do Atacadão (VTEX) com HTTP simulado — sem rede.
/// Cobrem: confirmação por EAN, preço unitário vs. condicionado (tiers),
/// matching por nome, variante errada e produto inexistente.
/// </summary>
public class AtacadaoPrecoCollectorTests
{
    private const string Ean = "7894900011517";

    private const string RegioesJson =
        """[{"sellers":[{"id":"atacadaobr733","name":"Atacadão Ferraz de Vasconcelos"}]}]""";

    private static string CatalogoJson(string nome, string itemId, string? ean)
    {
        var eanJson = ean is null ? "null" : "\"" + ean + "\"";
        return "[{\"productName\":\"" + nome + "\",\"link\":\"/produto-x/p\","
            + "\"items\":[{\"itemId\":\"" + itemId + "\",\"ean\":" + eanJson + ","
            + "\"sellers\":[{\"commertialOffer\":{\"Price\":0,\"AvailableQuantity\":0}}]}]}]";
    }

    private static string SimulacaoJson(int sellingPrice, string availability = "available",
        params (int Qtd, int Valor)[] tiers)
    {
        var tiersJson = tiers.Length == 0
            ? "[]"
            : "[" + string.Join(",", tiers.Select(t => "{\"quantity\":" + t.Qtd + ",\"value\":" + t.Valor + "}")) + "]";
        return "{\"items\":[{\"sellingPrice\":" + sellingPrice + ",\"availability\":\"" + availability
            + "\",\"priceDefinition\":{\"sellingPrices\":" + tiersJson + "}}]}";
    }

    private static AtacadaoPrecoCollector CriarColetor(FakeHttpHandler handler)
        => new(handler.CriarClient("https://www.atacadao.com.br/"),
            NullLogger<AtacadaoPrecoCollector>.Instance);

    private static FakeHttpHandler HandlerBase(string catalogoJson, string simulacaoJson)
        => new FakeHttpHandler()
            .Quando("products/search", catalogoJson)
            .Quando("regions", RegioesJson)
            .Quando("simulation", simulacaoJson);

    [Fact]
    public async Task Coletar_EanConfirmado_PrecoUnitario_ConfiancaHighERegiaoConfirmada()
    {
        var handler = HandlerBase(
            CatalogoJson("Refrigerante Coca-Cola 2L", "3870", Ean),
            SimulacaoJson(1249, tiers: [(1, 1249)]));

        var resultado = await CriarColetor(handler).ColetarAsync(Ean, "Coca-Cola 2L");

        resultado.Succeeded.Should().BeTrue();
        var coleta = resultado.Value!;
        coleta.Preco.Should().Be(12.49m);
        coleta.Disponivel.Should().BeTrue();
        coleta.Confianca.Should().Be(NivelConfiancaColeta.High);
        coleta.RegiaoConfirmada.Should().BeTrue();
        coleta.TipoPreco.Should().Be(TipoPrecoColeta.Normal);
    }

    [Fact]
    public async Task Coletar_ComDescontoPorQuantidade_DevolvePrecoCondicionado_MarcadoComoTal()
    {
        // Decisão de 2026-09-23: o site destaca o preço "a partir de N
        // unid." (ex.: Original 350ml: 5,19 → 4,99 em 12) — o coletor
        // devolve ESSE preço, marcado como CondicionadoQuantidade, nunca
        // misturado com o preço normal.
        var handler = HandlerBase(
            CatalogoJson("Refrigerante Coca-Cola 2L", "3870", Ean),
            SimulacaoJson(1229, tiers: [(1, 1249), (6, 1229)]));

        var resultado = await CriarColetor(handler).ColetarAsync(Ean, "Coca-Cola 2L");

        var coleta = resultado.Value!;
        coleta.Preco.Should().Be(12.29m, "o preço em destaque no site é o condicionado por quantidade");
        coleta.TipoPreco.Should().Be(TipoPrecoColeta.CondicionadoQuantidade);
    }

    [Fact]
    public async Task Coletar_SemEanNaResposta_CasaPorNome_ConfiancaMedium()
    {
        var handler = HandlerBase(
            CatalogoJson("Refrigerante Coca-Cola Pet 2L", "3870", ean: null),
            SimulacaoJson(1249, tiers: [(1, 1249)]));

        var resultado = await CriarColetor(handler).ColetarAsync(Ean, "Coca-Cola 2L");

        var coleta = resultado.Value!;
        coleta.Preco.Should().Be(12.49m);
        coleta.Confianca.Should().Be(NivelConfiancaColeta.Medium);
        coleta.RegiaoConfirmada.Should().BeTrue();
    }

    [Fact]
    public async Task Coletar_VarianteErradaSemEan_NaoAssociaPreco()
    {
        // Busca "Coca-Cola 2L", site devolve só a lata 350ml sem EAN:
        // o matcher deve rejeitar (medida obrigatória) e NÃO inventar preço.
        var handler = HandlerBase(
            CatalogoJson("Refrigerante Coca-Cola Lata 350ml", "9999", ean: null),
            SimulacaoJson(499, tiers: [(1, 499)]));

        var resultado = await CriarColetor(handler).ColetarAsync(Ean, "Coca-Cola 2L");

        var coleta = resultado.Value!;
        coleta.Preco.Should().BeNull();
        coleta.Disponivel.Should().BeFalse();
    }

    [Fact]
    public async Task Coletar_ProdutoInexistente_RetornaIndisponivelSemPreco()
    {
        var handler = HandlerBase("[]", SimulacaoJson(0));

        var resultado = await CriarColetor(handler).ColetarAsync(Ean, "Produto Que Nao Existe");

        resultado.Succeeded.Should().BeTrue();
        resultado.Value!.Preco.Should().BeNull();
        resultado.Value!.Disponivel.Should().BeFalse();
    }

    [Fact]
    public async Task Coletar_ItemIndisponivel_RetornaDisponivelFalse()
    {
        var handler = HandlerBase(
            CatalogoJson("Refrigerante Coca-Cola 2L", "3870", Ean),
            SimulacaoJson(1249, availability: "unavailable", tiers: [(1, 1249)]));

        var resultado = await CriarColetor(handler).ColetarAsync(Ean, "Coca-Cola 2L");

        resultado.Value!.Disponivel.Should().BeFalse();
    }
}
