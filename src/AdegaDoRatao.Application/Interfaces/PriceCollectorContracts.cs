using AdegaDoRatao.Application.Common;

namespace AdegaDoRatao.Application.Interfaces;

/// <summary>Resultado da coleta de um produto em uma rede.</summary>
public sealed record ColetaPrecoRede(
    string Rede,
    string? NomeProdutoNaRede,
    decimal? Preco,
    bool Disponivel);

/// <summary>
/// Porta de saída para um coletor de preços de uma rede específica
/// (Tenda, Atacadão, Shibata, Sonda). Cada implementação sabe como
/// consultar o site/API daquela rede por EAN, considerando o CEP da adega.
/// </summary>
public interface IPrecoRedeCollector
{
    /// <summary>Nome da rede (ex.: "Tenda", "Atacadão").</summary>
    string Rede { get; }

    /// <summary>
    /// Consulta o preço de um EAN na rede. Retorna Failure quando a coleta
    /// falhou (timeout, bloqueio, mudança de layout) — nunca lança exceção.
    /// </summary>
    Task<Result<ColetaPrecoRede>> ColetarAsync(string ean, CancellationToken cancellationToken = default);
}

/// <summary>
/// Caso de uso: atualização dos snapshots de preço de mercado, varrendo
/// os produtos com EAN cadastrado e consultando cada rede coletora.
/// </summary>
public interface IAtualizadorPrecosRedesService
{
    /// <summary>Atualiza os snapshots de todos os produtos com EAN. Retorna quantos EANs foram processados.</summary>
    Task<int> AtualizarTodosAsync(CancellationToken cancellationToken = default);

    /// <summary>Atualiza os snapshots de um único EAN (todas as redes).</summary>
    Task AtualizarPorEanAsync(string ean, CancellationToken cancellationToken = default);
}
