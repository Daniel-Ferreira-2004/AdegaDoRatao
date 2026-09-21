using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Interfaces;

namespace AdegaDoRatao.Application.Services;

/// <summary>
/// Caso de uso: comparação de preços de mercado de um produto do catálogo.
///
/// Usa o EAN (<see cref="Domain.Entities.Product.Barcode"/>) já cadastrado
/// no produto para consultar a API externa via <see cref="IPrecoMercadoExternoService"/>.
///
/// Falhas da API externa (timeout, indisponibilidade) NÃO viram exceção:
/// o caso de uso devolve uma resposta com ConsultaFalhou=true e todas as
/// redes marcadas como indisponíveis, para que o consumidor da API receba
/// 200 com a indicação clara de que não há dado — nunca um 500.
/// </summary>
public sealed class PrecoMercadoService : IPrecoMercadoService
{
    // A consulta é feita por ESTADO (SP), sem filtro de cidade: a Data
    // Market não tem cobertura de lojas em Suzano, então filtrar por cidade
    // sempre retornava vazio. O recorte regional fica por conta do filtro
    // de redes permitidas (RedesPermitidas abaixo).
    private const string? CidadePadrao = null;
    private const string EstadoPadrao = "SP";

    // Redes de supermercado aceitas no retorno. A comparação é por "contém",
    // sem acentos e em minúsculas, para cobrir variações da API externa
    // (ex.: "Assaí Atacadista", "D'Avó Supermercados", "Extra Hiper").
    private static readonly string[] RedesPermitidas =
    [
        "sonda", "shibata", "nagumo", "atacadao", "tenda", "rossi",
        "extra", "semar", "veran", "d'avo", "davo", "assai", "soni"
    ];

    // Cidades aceitas no retorno da comparação de preços: apenas lojas
    // da região de atuação da adega. A comparação é por "contém", sem
    // acentos e em minúsculas (ex.: "Poá" → "poa").
    private static readonly string[] CidadesPermitidas =
    [
        "suzano", "poa", "ferraz de vasconcelos", "gianette", "guaianases"
    ];

    private static bool RedePermitida(string rede)
    {
        var normalizado = Normalizar(rede);
        return RedesPermitidas.Any(normalizado.Contains);
    }

    private static bool CidadePermitida(string? cidade)
    {
        if (string.IsNullOrWhiteSpace(cidade))
        {
            return false;
        }

        var normalizado = Normalizar(cidade);
        return CidadesPermitidas.Any(normalizado.Contains);
    }

    /// <summary>
    /// Mensagem específica de indisponibilidade conforme o que o coletor
    /// conseguiu apurar: produto encontrado sem preço (provável falta de
    /// estoque) vs. produto não encontrado na busca da rede.
    /// </summary>
    private static string MensagemIndisponibilidade(MarketPriceSnapshot s)
        => (s.UrlProduto is not null, s.Preco is not null) switch
        {
            (true, false) => "Sem preço no site (provável falta de estoque).",
            (false, false) => "Não encontrado nesta rede.",
            _ => "Indisponível nesta rede."
        };

    private static string Normalizar(string texto)
    {
        var decomposto = texto.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder(decomposto.Length);
        foreach (var c in decomposto)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private readonly IProductRepository _products;
    private readonly IPrecoMercadoExternoService _externo;
    private readonly IMarketPriceSnapshotRepository _snapshots;

    public PrecoMercadoService(IProductRepository products, IPrecoMercadoExternoService externo,
        IMarketPriceSnapshotRepository snapshots)
        => (_products, _externo, _snapshots) = (products, externo, snapshots);

    public async Task<PrecoMercadoResponse> ConsultarAsync(Guid produtoId, CancellationToken cancellationToken = default)
    {
        var produto = await _products.ObterPorIdAsync(produtoId, cancellationToken)
            ?? throw new UseCaseException("Produto não encontrado.");

        if (string.IsNullOrWhiteSpace(produto.Barcode))
            throw new UseCaseException("O produto não possui código de barras (EAN) cadastrado.");

        // 1) Snapshots locais coletados pelo agente de preços (Tenda,
        //    Atacadão, Shibata, Sonda) — atualizados pelo job diário.
        var locais = await _snapshots.ListarPorEanAsync(produto.Barcode, cancellationToken);
        if (locais.Count > 0)
        {
            // Se há preço coletado, o produto ESTÁ disponível na rede —
            // alguns sites marcam isAvailable=false mesmo exibindo preço
            // (variação por loja), então o preço prevalece.
            var itens = locais
                .Select(s => new PrecoMercadoRedeResponse(
                    s.Rede, null, s.Preco,
                    s.Disponivel || s.Preco is not null,
                    s.Disponivel || s.Preco is not null ? null : MensagemIndisponibilidade(s),
                    DistanciaKm: null,
                    UrlProduto: s.UrlProduto))
                .OrderBy(p => p.Disponivel ? 0 : 1)
                .ThenBy(p => p.Preco ?? decimal.MaxValue)
                .ToArray();

            return new PrecoMercadoResponse(produto.Id, produto.Name, produto.Barcode,
                DateTime.UtcNow, OrigemCache: true, ConsultaFalhou: false, itens);
        }

        // 2) Fallback: API externa Data Market (quando o agente ainda não
        //    coletou este EAN).
        var resultado = await _externo.ConsultarPrecosAsync(
            produto.Barcode, CidadePadrao, EstadoPadrao, cancellationToken);

        if (!resultado.Succeeded)
        {
            return new PrecoMercadoResponse(produto.Id, produto.Name, produto.Barcode,
                DateTime.UtcNow, OrigemCache: false, ConsultaFalhou: true,
                Array.Empty<PrecoMercadoRedeResponse>());
        }

        // Enriquece com a distância de Ferraz de Vasconcelos até a cidade
        // da loja e ordena de forma "inteligente": disponíveis primeiro,
        // depois pelo menor preço; em empate de preço, o mais perto ganha.
        // Lojas sem preço vão para o final, da mais próxima à mais distante.
        var filtrados = resultado.Value!.Precos
            .Where(p => RedePermitida(p.Rede) && CidadePermitida(p.Cidade))
            .Select(p => p with { DistanciaKm = CidadesRegiao.DistanciaDeFerrazKm(p.Cidade) })
            .OrderBy(p => p.Disponivel ? 0 : 1)
            .ThenBy(p => p.Preco ?? decimal.MaxValue)
            .ThenBy(p => p.DistanciaKm ?? double.MaxValue)
            .ToArray();

        return new PrecoMercadoResponse(produto.Id, produto.Name, produto.Barcode,
            DateTime.UtcNow, resultado.Value.OrigemCache, ConsultaFalhou: false,
            filtrados);
    }
}
