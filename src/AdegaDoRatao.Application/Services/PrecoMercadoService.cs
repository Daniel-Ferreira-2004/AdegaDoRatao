using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
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
    // Redes padrão da região de Suzano/SP. O cruzamento com o retorno da API
    // é feito por correspondência de nome de loja (store_name) — ver
    // DataMarketPrecoService na Infrastructure.
    // TODO: validar com a Cnova se essas redes têm cobertura na base deles
    // (na consulta de 16/09/2026 nenhuma delas apareceu para Suzano/SP).
    private static readonly IReadOnlyCollection<string> RedesPadrao =
        new[] { "Veran", "Shibata", "Atacadão", "Semar" };

    // A consulta é feita por ESTADO (SP), sem filtro de cidade: assim cada
    // rede retorna o menor preço encontrado na região (Suzano, Poá, Ferraz
    // de Vasconcelos etc.), com a cidade indicada no item da resposta.
    private const string? CidadePadrao = null;
    private const string EstadoPadrao = "SP";

    private readonly IProductRepository _products;
    private readonly IPrecoMercadoExternoService _externo;

    public PrecoMercadoService(IProductRepository products, IPrecoMercadoExternoService externo)
        => (_products, _externo) = (products, externo);

    public async Task<PrecoMercadoResponse> ConsultarAsync(Guid produtoId, CancellationToken cancellationToken = default)
    {
        var produto = await _products.ObterPorIdAsync(produtoId, cancellationToken)
            ?? throw new UseCaseException("Produto não encontrado.");

        if (string.IsNullOrWhiteSpace(produto.Barcode))
            throw new UseCaseException("O produto não possui código de barras (EAN) cadastrado.");

        var resultado = await _externo.ConsultarPrecosAsync(
            produto.Barcode, CidadePadrao, EstadoPadrao, RedesPadrao, cancellationToken);

        if (!resultado.Succeeded)
        {
            var indisponiveis = RedesPadrao
                .Select(rede => new PrecoMercadoRedeResponse(rede, CidadePadrao, null, false, resultado.Error))
                .ToArray();
            return new PrecoMercadoResponse(produto.Id, produto.Name, produto.Barcode,
                DateTime.UtcNow, OrigemCache: false, ConsultaFalhou: true, indisponiveis);
        }

        return new PrecoMercadoResponse(produto.Id, produto.Name, produto.Barcode,
            DateTime.UtcNow, resultado.Value!.OrigemCache, ConsultaFalhou: false,
            resultado.Value.Precos);
    }
}
