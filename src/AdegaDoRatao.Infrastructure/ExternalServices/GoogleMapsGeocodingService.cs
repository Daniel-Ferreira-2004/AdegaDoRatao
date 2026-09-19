using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdegaDoRatao.Infrastructure.ExternalServices;

/// <summary>
/// Implementação de <see cref="IGeocodingService"/> contra a Google Maps
/// Geocoding API — precisão superior ao Nominatim para endereços brasileiros.
///
/// Endpoint consumido:
///   GET {BaseUrl}/maps/api/geocode/json?address={endereco}&region=br&language=pt-BR&key={ApiKey}
///
/// A chave NUNCA fica no appsettings.json:
///   dotnet user-secrets set "Geocoding:ApiKey" "SUA_CHAVE" --project src/AdegaDoRatao.API
///   ou variável de ambiente Geocoding__ApiKey.
///
/// Comportamento de borda (mesmo contrato do NominatimGeocodingService):
///  - ZERO_RESULTS → Result.Failure "endereço não encontrado";
///  - OVER_QUERY_LIMIT / REQUEST_DENIED / erro de rede / timeout →
///    Result.Failure com mensagem clara (o caso de uso converte em
///    UseCaseException → HTTP 400, nunca 500).
/// </summary>
public sealed class GoogleMapsGeocodingService : IGeocodingService
{
    private readonly HttpClient _http;
    private readonly GeocodingOptions _options;
    private readonly ILogger<GoogleMapsGeocodingService> _logger;

    public GoogleMapsGeocodingService(
        HttpClient http,
        IOptions<GeocodingOptions> options,
        ILogger<GoogleMapsGeocodingService> logger)
        => (_http, _options, _logger) = (http, options.Value, logger);

    public async Task<Result<Coordenadas>> GeocodificarAsync(string endereco, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogError("Geocoding:ApiKey não configurada. Configure via user-secrets ou variável de ambiente.");
            return Result<Coordenadas>.Failure(
                "O serviço de localização não está configurado. Contate o suporte.");
        }

        var url = $"maps/api/geocode/json?address={Uri.EscapeDataString(endereco)}" +
                  $"&region=br&language=pt-BR&key={_options.ApiKey}";

        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google Geocoding falhou com HTTP {Status} para '{Endereco}'.",
                    (int)response.StatusCode, endereco);
                return Result<Coordenadas>.Failure(
                    "O serviço de localização está indisponível no momento. Tente novamente em instantes.");
            }

            var payload = await response.Content.ReadFromJsonAsync<GoogleGeocodeResponse>(cancellationToken);

            switch (payload?.Status)
            {
                case "OK" when payload.Results is { Count: > 0 }:
                    var location = payload.Results[0].Geometry?.Location;
                    if (location is null)
                    {
                        return Result<Coordenadas>.Failure(
                            "O serviço de localização retornou coordenadas inválidas para o endereço informado.");
                    }
                    return Result<Coordenadas>.Success(new Coordenadas(location.Lat, location.Lng));

                case "ZERO_RESULTS":
                    return Result<Coordenadas>.Failure(
                        "Endereço não encontrado. Verifique o endereço ou CEP informado e tente novamente.");

                case "OVER_QUERY_LIMIT":
                    _logger.LogWarning("Google Geocoding: cota excedida.");
                    return Result<Coordenadas>.Failure(
                        "O limite de consultas de localização foi atingido. Tente novamente mais tarde.");

                case "REQUEST_DENIED":
                    _logger.LogError("Google Geocoding: requisição negada — verifique a chave de API. {Erro}",
                        payload?.ErrorMessage);
                    return Result<Coordenadas>.Failure(
                        "O serviço de localização está indisponível no momento. Tente novamente em instantes.");

                default:
                    _logger.LogWarning("Google Geocoding retornou status inesperado '{Status}' para '{Endereco}'.",
                        payload?.Status, endereco);
                    return Result<Coordenadas>.Failure(
                        "O serviço de localização está indisponível no momento. Tente novamente em instantes.");
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<Coordenadas>.Failure(
                "A consulta de localização excedeu o tempo limite. Tente novamente.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Erro de rede ao geocodificar '{Endereco}' via Google.", endereco);
            return Result<Coordenadas>.Failure(
                "Não foi possível contactar o serviço de localização. Tente novamente em instantes.");
        }
    }

    private sealed class GoogleGeocodeResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; init; }

        [JsonPropertyName("results")]
        public List<GoogleGeocodeResult>? Results { get; init; }
    }

    private sealed class GoogleGeocodeResult
    {
        [JsonPropertyName("geometry")]
        public GoogleGeometry? Geometry { get; init; }
    }

    private sealed class GoogleGeometry
    {
        [JsonPropertyName("location")]
        public GoogleLocation? Location { get; init; }
    }

    private sealed class GoogleLocation
    {
        [JsonPropertyName("lat")]
        public double Lat { get; init; }

        [JsonPropertyName("lng")]
        public double Lng { get; init; }
    }
}
