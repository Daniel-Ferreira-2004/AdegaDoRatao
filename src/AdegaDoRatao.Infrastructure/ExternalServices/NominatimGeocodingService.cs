using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdegaDoRatao.Infrastructure.ExternalServices;

/// <summary>
/// Implementação de <see cref="IGeocodingService"/> contra o Nominatim
/// (OpenStreetMap) — gratuito, sem chave de API.
///
/// Endpoint consumido:
///   GET {BaseUrl}/search?q={endereco}&format=json&limit=1&countrycodes=br
///   Header obrigatório: User-Agent (política de uso do OSM).
///
/// Comportamento de borda:
///  - endereço não encontrado (lista vazia) → Result.Failure com mensagem clara;
///  - timeout / erro de rede / 5xx → Result.Failure (o caso de uso converte
///    em UseCaseException → HTTP 400, nunca 500).
/// </summary>
public sealed class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _http;
    private readonly GeocodingOptions _options;
    private readonly ILogger<NominatimGeocodingService> _logger;

    public NominatimGeocodingService(
        HttpClient http,
        IOptions<GeocodingOptions> options,
        ILogger<NominatimGeocodingService> logger)
        => (_http, _options, _logger) = (http, options.Value, logger);

    public async Task<Result<Coordenadas>> GeocodificarAsync(string endereco, CancellationToken cancellationToken = default)
    {
        var url = $"search?q={Uri.EscapeDataString(endereco)}&format=json&limit=1&countrycodes=br";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Geocodificação falhou com HTTP {Status} para '{Endereco}'.",
                    (int)response.StatusCode, endereco);
                return Result<Coordenadas>.Failure(
                    "O serviço de localização está indisponível no momento. Tente novamente em instantes.");
            }

            var resultados = await response.Content.ReadFromJsonAsync<List<NominatimResult>>(cancellationToken);
            var primeiro = resultados?.FirstOrDefault();
            if (primeiro is null)
            {
                return Result<Coordenadas>.Failure(
                    "Endereço não encontrado. Verifique o endereço ou CEP informado e tente novamente.");
            }

            if (!double.TryParse(primeiro.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
                || !double.TryParse(primeiro.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
            {
                return Result<Coordenadas>.Failure(
                    "O serviço de localização retornou coordenadas inválidas para o endereço informado.");
            }

            return Result<Coordenadas>.Success(new Coordenadas(lat, lng));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<Coordenadas>.Failure(
                "A consulta de localização excedeu o tempo limite. Tente novamente.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Erro de rede ao geocodificar '{Endereco}'.", endereco);
            return Result<Coordenadas>.Failure(
                "Não foi possível contactar o serviço de localização. Tente novamente em instantes.");
        }
    }

    private sealed class NominatimResult
    {
        [JsonPropertyName("lat")]
        public string? Lat { get; init; }

        [JsonPropertyName("lon")]
        public string? Lon { get; init; }
    }
}
