using AdegaDoRatao.Domain.Common;
using AdegaDoRatao.Domain.Enums;

namespace AdegaDoRatao.Domain.Entities;

/// <summary>
/// Snapshot de preço de um produto (por EAN) em uma rede de mercado,
/// coletado pelo agente de preços (coletores por rede: Tenda, Atacadão,
/// Shibata, Sonda). Diferente da consulta efêmera à Data Market, este dado
/// é persistido para permitir histórico e consulta instantânea.
/// </summary>
public class MarketPriceSnapshot : BaseEntity
{
    public string Ean { get; private set; } = string.Empty;
    public string Rede { get; private set; } = string.Empty;
    public string? NomeProdutoNaRede { get; private set; }
    public decimal? Preco { get; private set; }
    public bool Disponivel { get; private set; }
    public DateTime ColetadoEm { get; private set; }

    /// <summary>URL da página do produto no site da rede (para o botão "Ver no site").</summary>
    public string? UrlProduto { get; private set; }

    /// <summary>Classificação do preço (normal, promocional, condicionado etc.).</summary>
    public TipoPrecoColeta TipoPreco { get; private set; } = TipoPrecoColeta.Normal;

    /// <summary>Nível de confiança do dado coletado.</summary>
    public NivelConfiancaColeta Confianca { get; private set; } = NivelConfiancaColeta.Unverified;

    /// <summary>true quando o coletor confirmou que o preço é da região/loja da adega.</summary>
    public bool RegiaoConfirmada { get; private set; }

    private MarketPriceSnapshot()
    {
    }

    public MarketPriceSnapshot(string ean, string rede, string? nomeProdutoNaRede,
        decimal? preco, bool disponivel, string? urlProduto = null,
        TipoPrecoColeta tipoPreco = TipoPrecoColeta.Normal,
        NivelConfiancaColeta confianca = NivelConfiancaColeta.Unverified,
        bool regiaoConfirmada = false)
    {
        if (string.IsNullOrWhiteSpace(ean))
        {
            throw new ArgumentException("O EAN é obrigatório.", nameof(ean));
        }

        if (string.IsNullOrWhiteSpace(rede))
        {
            throw new ArgumentException("A rede é obrigatória.", nameof(rede));
        }

        Ean = ean.Trim();
        Rede = rede.Trim();
        NomeProdutoNaRede = string.IsNullOrWhiteSpace(nomeProdutoNaRede) ? null : nomeProdutoNaRede.Trim();
        Preco = preco;
        Disponivel = disponivel;
        UrlProduto = string.IsNullOrWhiteSpace(urlProduto) ? null : urlProduto.Trim();
        TipoPreco = tipoPreco;
        Confianca = confianca;
        RegiaoConfirmada = regiaoConfirmada;
        ColetadoEm = DateTime.UtcNow;
    }

    public void Atualizar(decimal? preco, bool disponivel, string? nomeProdutoNaRede, string? urlProduto = null,
        TipoPrecoColeta tipoPreco = TipoPrecoColeta.Normal,
        NivelConfiancaColeta confianca = NivelConfiancaColeta.Unverified,
        bool regiaoConfirmada = false)
    {
        Preco = preco;
        Disponivel = disponivel;
        NomeProdutoNaRede = string.IsNullOrWhiteSpace(nomeProdutoNaRede) ? NomeProdutoNaRede : nomeProdutoNaRede.Trim();
        UrlProduto = string.IsNullOrWhiteSpace(urlProduto) ? UrlProduto : urlProduto.Trim();
        TipoPreco = tipoPreco;
        Confianca = confianca;
        RegiaoConfirmada = regiaoConfirmada;
        ColetadoEm = DateTime.UtcNow;
        MarcarComoAtualizada();
    }
}
