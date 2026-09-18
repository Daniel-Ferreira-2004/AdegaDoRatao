using AdegaDoRatao.Domain.Common;

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

    private MarketPriceSnapshot()
    {
    }

    public MarketPriceSnapshot(string ean, string rede, string? nomeProdutoNaRede,
        decimal? preco, bool disponivel)
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
        ColetadoEm = DateTime.UtcNow;
    }

    public void Atualizar(decimal? preco, bool disponivel, string? nomeProdutoNaRede)
    {
        Preco = preco;
        Disponivel = disponivel;
        NomeProdutoNaRede = string.IsNullOrWhiteSpace(nomeProdutoNaRede) ? NomeProdutoNaRede : nomeProdutoNaRede.Trim();
        ColetadoEm = DateTime.UtcNow;
        MarcarComoAtualizada();
    }
}
