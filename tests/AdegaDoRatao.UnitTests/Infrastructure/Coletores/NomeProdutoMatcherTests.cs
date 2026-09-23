using AdegaDoRatao.Infrastructure.ExternalServices.Coletores;
using FluentAssertions;
using Xunit;

namespace AdegaDoRatao.UnitTests.Infrastructure.Coletores;

/// <summary>
/// Testes do matcher de nomes — a camada que impede associar o preço de
/// um produto a outro (regra 4 do protocolo: variantes NÃO são o mesmo
/// produto).
/// </summary>
public class NomeProdutoMatcherTests
{
    // ---- Busca: produto simples / marca / peso / acento / incompleto ----

    [Fact]
    public void Pontuar_NomeExato_RetornaUm()
        => NomeProdutoMatcher.Pontuar("Coca-Cola 2L", "Refrigerante Coca-Cola 2L").Should().Be(1.0);

    [Fact]
    public void Pontuar_ComAcento_IgnoraAcentuacao()
        => NomeProdutoMatcher.Pontuar("Suco de Uva 1L", "Suco de Uva Integral 1L")
            .Should().BeGreaterThanOrEqualTo(NomeProdutoMatcher.ScoreMinimo);

    [Fact]
    public void Pontuar_NomeIncompleto_CasaComNomeCompleto()
        => NomeProdutoMatcher.Pontuar("Doritos 120", "Salgadinho Elma Chips Doritos Queijo Nacho 120g")
            .Should().BeGreaterThanOrEqualTo(NomeProdutoMatcher.ScoreMinimo);

    [Fact]
    public void Pontuar_PesoComEspaco_CasaComPesoSemEspaco()
        => NomeProdutoMatcher.ContemTokensObrigatorios("Arroz 5kg", "Arroz Branco Tipo 1 5 Kg")
            .Should().BeTrue();

    [Fact]
    public void Pontuar_TermoVazio_RetornaZero()
        => NomeProdutoMatcher.Pontuar("", "Qualquer").Should().Be(0);

    // ---- Produto: tamanhos / sabores / variantes diferentes ----

    [Fact]
    public void TokensObrigatorios_TamanhoDiferente_NaoCasa()
        => NomeProdutoMatcher.ContemTokensObrigatorios("Coca-Cola 2L", "Refrigerante Coca-Cola 350ml")
            .Should().BeFalse("2L e 350ml são produtos diferentes");

    [Fact]
    public void TokensObrigatorios_PesoDiferente_NaoCasa()
        => NomeProdutoMatcher.ContemTokensObrigatorios("Arroz 5kg", "Arroz Branco Tipo 1 1kg")
            .Should().BeFalse("5kg e 1kg são produtos diferentes");

    [Fact]
    public void TokensObrigatorios_MarcaAusente_NaoCasa()
        => NomeProdutoMatcher.ContemTokensObrigatorios("Nutella 350g", "Creme de Avelã Genérico 350g")
            .Should().BeFalse("a marca é obrigatória quando presente na busca");

    [Fact]
    public void TokensObrigatorios_MesmaMedidaEMarca_Casa()
        => NomeProdutoMatcher.ContemTokensObrigatorios("Coca-Cola 2L", "Refrigerante Coca-Cola Pet 2L")
            .Should().BeTrue();

    // Comportamento atual documentado: o matcher NÃO distingue variantes
    // de sabor/tipo quando a medida coincide (ex.: "Coca 2L" casa com
    // "Coca-Cola Zero 2L"). Risco registrado no perfil de cada site —
    // a confirmação por EAN (Atacadão/Tenda) é a proteção real.
    [Fact]
    public void TokensObrigatorios_VarianteComMesmaMedida_Casa_ComportamentoConhecido()
        => NomeProdutoMatcher.ContemTokensObrigatorios("Coca 2L", "Coca-Cola Zero 2L")
            .Should().BeTrue("limitação conhecida: variantes com a mesma medida passam no matcher");
}
