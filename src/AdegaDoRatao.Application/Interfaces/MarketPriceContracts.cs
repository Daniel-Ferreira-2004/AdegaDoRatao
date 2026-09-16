using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;

namespace AdegaDoRatao.Application.Interfaces;

/// <summary>
/// Porta de saída para a API externa de comparação de preços de mercado
/// (Cnova Tech Data Market). Implementada na Infrastructure via HttpClient.
///
/// O contrato devolve <see cref="Result{T}"/> em vez de lançar exceção:
/// timeout, erro HTTP e "produto não encontrado" são situações ESPERADAS
/// de uma integração externa e devem ser representadas no retorno, não
/// propagadas como falha não tratada.
/// </summary>
/// <summary>Resultado da consulta à API externa, já indicando se veio do cache.</summary>
public sealed record ConsultaExternaPrecos(
    IReadOnlyList<PrecoMercadoRedeResponse> Precos,
    bool OrigemCache);

public interface IPrecoMercadoExternoService
{
    /// <summary>
    /// Consulta o preço de um EAN em todas as lojas da região (estado/cidade),
    /// sem filtro de rede — cada loja encontrada vira um item da resposta.
    /// Retorna Failure apenas em falha total da consulta (timeout, erro de
    /// rede, 5xx). Produto não encontrado (404) devolve lista vazia.
    /// </summary>
    Task<Result<ConsultaExternaPrecos>> ConsultarPrecosAsync(
        string ean,
        string? cidade,
        string? estado,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Caso de uso: comparação de preços de mercado de um produto do catálogo,
/// usando o EAN (Barcode) já cadastrado na entidade Product.
/// </summary>
public interface IPrecoMercadoService
{
    Task<PrecoMercadoResponse> ConsultarAsync(Guid produtoId, CancellationToken cancellationToken = default);
}
