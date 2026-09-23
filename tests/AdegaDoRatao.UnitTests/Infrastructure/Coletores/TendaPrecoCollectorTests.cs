using AdegaDoRatao.Domain.Enums;
using AdegaDoRatao.Infrastructure.ExternalServices.Coletores;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AdegaDoRatao.UnitTests.Infrastructure.Coletores;

/// <summary>
/// Testes do coletor do Tenda (Next.js __NEXT_DATA__) com HTTP simulado.
/// Cobrem: EAN na thumbnail, matching por nome, variante errada,
/// produto inexistente e mudança de estrutura da página.
/// </summary>
public class TendaPrecoCollectorTests
{
    private const string Ean = "7891234567890";

    private static string PaginaBusca(string produtosJson)
        => "<html><body><script id=\"__NEXT_DATA__\" type=\"application/json\">"
            + "{\"props\":{\"pageProps\":{\"products\":" + produtosJson + "}}}"
            + "</script></body></html>";

    private static TendaPrecoCollector CriarColetor(FakeHttpHandler handler)
        => new(handler.CriarClient("https://www.tendaatacado.com.br/"),
            NullLogger<TendaPrecoCollector>.Instance);

    [Fact]
    public async Task Coletar_EanNaThumbnail_ConfirmaProduto_ConfiancaMedium()
    {
        var html = PaginaBusca(
            "[{\"name\":\"Arroz Branco Tipo 1 5kg\",\"price\":22.9,\"isAvailable\":true,"
            + "\"thumbnail\":\"https://cdn.tenda.com/img/" + Ean + ".jpg\",\"slug\":\"arroz-5kg\"}]");
        var handler = new FakeHttpHandler().QuandoHtml("busca", html);

        var resultado = await CriarColetor(handler).ColetarAsync(Ean, "Arroz 5kg");

        var coleta = resultado.Value!;
        coleta.Preco.Should().Be(22.9m);
        coleta.Disponivel.Should().BeTrue();
        coleta.UrlProduto.Should().Be("https://www.tendaatacado.com.br/produto/arroz-5kg");
        coleta.Confianca.Should().Be(NivelConfiancaColeta.Medium);
        coleta.RegiaoConfirmada.Should().BeFalse("o Tenda não valida o CEP da adega");
    }

    [Fact]
    public async Task Coletar_SemEan_CasaPorNome_ConfiancaLow()
    {
        var html = PaginaBusca(
            """[{"name":"Arroz Branco Tipo 1 5kg","price":22.9,"isAvailable":true,"thumbnail":"https://cdn.tenda.com/img/sem-ean.jpg","slug":"arroz-5kg"}]""");
        var handler = new FakeHttpHandler().QuandoHtml("busca", html);

        var resultado = await CriarColetor(handler).ColetarAsync(Ean, "Arroz 5kg");

        var coleta = resultado.Value!;
        coleta.Preco.Should().Be(22.9m);
        coleta.Confianca.Should().Be(NivelConfiancaColeta.Low);
    }

    [Fact]
    public async Task Coletar_VarianteErrada_NaoAssociaPreco()
    {
        // Busca "Arroz 5kg", site devolve só o pacote de 1kg.
        var html = PaginaBusca(
            """[{"name":"Arroz Branco Tipo 1 1kg","price":5.49,"isAvailable":true,"slug":"arroz-1kg"}]""");
        var handler = new FakeHttpHandler().QuandoHtml("busca", html);

        var resultado = await CriarColetor(handler).ColetarAsync(Ean, "Arroz 5kg");

        resultado.Value!.Preco.Should().BeNull();
        resultado.Value!.Disponivel.Should().BeFalse();
    }

    [Fact]
    public async Task Coletar_ProdutoInexistente_RetornaIndisponivel()
    {
        var handler = new FakeHttpHandler().QuandoHtml("busca", PaginaBusca("[]"));

        var resultado = await CriarColetor(handler).ColetarAsync(Ean, "Produto Inexistente");

        resultado.Value!.Preco.Should().BeNull();
        resultado.Value!.Disponivel.Should().BeFalse();
    }

    [Fact]
    public async Task Coletar_ComWholesalePrice_DevolvePrecoCondicionado_MarcadoComoTal()
    {
        // Estrutura real do Tenda (2026-09-23): "price":4.99,
        // "wholesalePrices":[{"minQuantity":6,"price":4.79}] — o site
        // destaca "a partir de 6 unidades R$ 4,79".
        var html = PaginaBusca(
            "[{\"name\":\"Cerveja Pilsen Original 350ml\",\"price\":4.99,\"isAvailable\":true,"
            + "\"wholesalePrices\":[{\"minQuantity\":6,\"price\":4.79}],\"slug\":\"original-350\"}]");
        var handler = new FakeHttpHandler().QuandoHtml("busca", html);

        var resultado = await CriarColetor(handler).ColetarAsync(Ean, "Original 350ml");

        var coleta = resultado.Value!;
        coleta.Preco.Should().Be(4.79m, "o preço em destaque no site é o condicionado por quantidade");
        coleta.TipoPreco.Should().Be(TipoPrecoColeta.CondicionadoQuantidade);
    }

    [Fact]
    public async Task Coletar_SemNextData_RetornaFalha_SiteStructureChanged()
    {
        // A página mudou e o bloco __NEXT_DATA__ sumiu: o coletor deve
        // falhar explicitamente (nunca inventar preço).
        var handler = new FakeHttpHandler().QuandoHtml("busca", "<html><body>nova home</body></html>");

        var resultado = await CriarColetor(handler).ColetarAsync(Ean, "Arroz 5kg");

        resultado.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Coletar_ComSessaoRegionalizada_RegiaoConfirmada_ConfiancaHighComEan()
    {
        // Fluxo real mapeado em 2026-09-23: anonymous-client → access-token
        // → client → shopping-cart {zipcode} → cookies _Tendaatacado-*.
        var html = PaginaBusca(
            "[{\"name\":\"Arroz Branco Tipo 1 5kg\",\"price\":22.9,\"isAvailable\":true,"
            + "\"thumbnail\":\"https://cdn.tenda.com/img/" + Ean + ".jpg\",\"slug\":\"arroz-5kg\"}]");
        var handler = new FakeHttpHandler()
            .Quando("anonymous-client", "{\"user\":\"u\",\"password\":\"p\"}")
            .Quando("access-token", "{\"access_token\":\"tok\",\"refresh_token\":\"r\",\"expires_in\":10083}")
            .Quando("api/client", "{\"id\":123}")
            .Quando("shopping-cart", "{\"shoppingCartId\":456,\"branchId\":\"41\"}")
            .QuandoHtml("busca", html);

        var resultado = await CriarColetor(handler).ColetarAsync(Ean, "Arroz 5kg");

        var coleta = resultado.Value!;
        coleta.Preco.Should().Be(22.9m);
        coleta.RegiaoConfirmada.Should().BeTrue("a sessão anônima com o CEP da adega regionaliza o preço");
        coleta.Confianca.Should().Be(NivelConfiancaColeta.High);

        // A busca deve ter levado os cookies de sessão regionalizados.
        var busca = handler.Requisicoes.Last(r => r.RequestUri!.ToString().Contains("busca"));
        busca.Headers.GetValues("Cookie").Should().ContainSingle()
            .Which.Should().Contain("_Tendaatacado-branchID=41")
            .And.Contain("_Tendaatacado-cartID=456")
            .And.Contain("_Tendaatacado-userInfo=123")
            .And.Contain("_Tendaatacado-auth-token!");
    }

    [Fact]
    public async Task Coletar_SessaoRegionalFalha_ColetaSemRegiao_ConfiancaDegrada()
    {
        // Se a API de sessão falhar (404 no handler), a coleta NÃO quebra:
        // segue sem regionalização, com RegiaoConfirmada=false.
        var html = PaginaBusca(
            "[{\"name\":\"Arroz Branco Tipo 1 5kg\",\"price\":22.9,\"isAvailable\":true,"
            + "\"thumbnail\":\"https://cdn.tenda.com/img/" + Ean + ".jpg\",\"slug\":\"arroz-5kg\"}]");
        var handler = new FakeHttpHandler().QuandoHtml("busca", html);

        var resultado = await CriarColetor(handler).ColetarAsync(Ean, "Arroz 5kg");

        var coleta = resultado.Value!;
        coleta.Preco.Should().Be(22.9m);
        coleta.RegiaoConfirmada.Should().BeFalse();
        coleta.Confianca.Should().Be(NivelConfiancaColeta.Medium);
    }
}
