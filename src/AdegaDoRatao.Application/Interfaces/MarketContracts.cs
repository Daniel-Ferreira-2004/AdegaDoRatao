using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;

namespace AdegaDoRatao.Application.Interfaces;

/// <summary>Coordenadas geográficas resultantes de uma geocodificação.</summary>
public sealed record Coordenadas(double Latitude, double Longitude);

/// <summary>
/// Porta de saída para o serviço de geocodificação (endereço/CEP →
/// latitude/longitude). Implementada na Infrastructure via HttpClient
/// (Nominatim/OpenStreetMap por padrão — gratuito, sem chave de API).
///
/// Devolve <see cref="Result{T}"/> em vez de lançar exceção: endereço
/// não encontrado, timeout e indisponibilidade são situações ESPERADAS
/// de uma integração externa e devem ser representadas no retorno.
/// </summary>
public interface IGeocodingService
{
    /// <summary>
    /// Converte um endereço ou CEP em coordenadas. Retorna Failure quando
    /// o endereço não é encontrado ou o serviço está indisponível.
    /// </summary>
    Task<Result<Coordenadas>> GeocodificarAsync(string endereco, CancellationToken cancellationToken = default);
}

/// <summary>
/// Caso de uso: busca de mercados próximos a um ponto (ou a um endereço,
/// geocodificado internamente), ordenados pela distância.
/// </summary>
public interface IMercadoService
{
    /// <summary>Busca por coordenadas já conhecidas (ex.: GPS do dispositivo).</summary>
    Task<IReadOnlyList<MercadoProximoResponse>> BuscarProximosAsync(
        BuscarMercadosProximosQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca a partir de um endereço/CEP em texto livre. Falhas de
    /// geocodificação viram <see cref="UseCaseException"/> com mensagem
    /// clara para o frontend (HTTP 400), nunca 500.
    /// </summary>
    Task<IReadOnlyList<MercadoProximoResponse>> BuscarProximosPorEnderecoAsync(
        string endereco, double raioKm = 5.0, CancellationToken cancellationToken = default);
}
