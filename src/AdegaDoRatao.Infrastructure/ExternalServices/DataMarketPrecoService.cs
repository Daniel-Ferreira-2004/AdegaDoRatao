using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdegaDoRatao.Infrastructure.ExternalServices;

/// <summary>
/// Implementação de <see cref="IPrecoMercadoExternoService"/> contra a
/// Cnova Tech Data Market (https://datamarket.cnovatech.com.br).
///
/// Endpoint consumido:
///   GET {BaseUrl}/api/v1/market/products/ean/{ean}
///   Header: X-API-Key: {DataMarket:ApiKey}
///
/// Comportamento de borda:
///  - 404 → sucesso com todas as redes indisponíveis ("produto não encontrado");
///  - timeout / erro de rede / 5xx → Result.Failure (o caso de uso converte
///    em resposta "consulta falhou", sem exceção não tratada);
///  - respostas bem-sucedidas ficam em IMemoryCache por DataMarket:CacheHours
///    (padrão 6h) para proteger a cota de 50 consultas/mês do plano Free.
/// </summary>
public sealed class DataMarketPrecoService : IPrecoMercadoExternoService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly DataMarketOptions _options;
    private readonly ILogger<DataMarketPrecoService> _logger;

    public DataMarketPrecoService(
        HttpClient http,
        IMemoryCache cache,
        IOptions<DataMarketOptions> options,
        ILogger<DataMarketPrecoService> logger)
        => (_http, _cache, _options, _logger) = (http, cache, options.Value, logger);

    public async Task<Result<ConsultaExternaPrecos>> ConsultarPrecosAsync(
        string ean,
        string? cidade,
        string? estado,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return Result<ConsultaExternaPrecos>.Failure(
                "Integração de preços de mercado não configurada (DataMarket:ApiKey ausente).");

        var cacheKey = $"precos-mercado:{ean}:{cidade}:{estado}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<PrecoMercadoRedeResponse>? cached) && cached is not null)
            return Result<ConsultaExternaPrecos>.Success(new ConsultaExternaPrecos(cached, OrigemCache: true));

        var resultado = await ChamarApiAsync(ean, cidade, estado, cancellationToken);
        if (resultado.Succeeded)
        {
            _cache.Set(cacheKey, resultado.Value!.Precos, TimeSpan.FromHours(_options.CacheHours));
        }
        return resultado;
    }

    private async Task<Result<ConsultaExternaPrecos>> ChamarApiAsync(
        string ean, string? cidade, string? estado, CancellationToken cancellationToken)
    {
        // Filtros confirmados contra a API real em 16/09/2026: os query params
        // "state" e "city" funcionam (ex.: ?state=SP&city=Suzano). Sem eles a
        // API devolve resultados de todo o Brasil.
        var url = $"/api/v1/market/products/ean/{Uri.EscapeDataString(ean)}";
        var filtros = new List<string>();
        if (!string.IsNullOrWhiteSpace(estado)) filtros.Add($"state={Uri.EscapeDataString(estado)}");
        if (!string.IsNullOrWhiteSpace(cidade)) filtros.Add($"city={Uri.EscapeDataString(cidade)}");
        if (filtros.Count > 0) url += "?" + string.Join('&', filtros);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-API-Key", _options.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Timeout ao consultar preços de mercado para o EAN {Ean}.", ean);
            return Result<ConsultaExternaPrecos>.Failure("A consulta à API de preços de mercado excedeu o tempo limite.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Falha de rede ao consultar preços de mercado para o EAN {Ean}.", ean);
            return Result<ConsultaExternaPrecos>.Failure("Falha de comunicação com a API de preços de mercado.");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            // Produto não encontrado na base: lista vazia, sem erro.
            return Result<ConsultaExternaPrecos>.Success(
                new ConsultaExternaPrecos(Array.Empty<PrecoMercadoRedeResponse>(), OrigemCache: false));
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("API de preços de mercado respondeu {StatusCode} para o EAN {Ean}.",
                (int)response.StatusCode, ean);
            return Result<ConsultaExternaPrecos>.Failure(
                $"A API de preços de mercado respondeu com erro ({(int)response.StatusCode}).");
        }

        DataMarketProductResponse? payload;
        try
        {
            payload = await response.Content.ReadFromJsonAsync<DataMarketProductResponse>(JsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Resposta inesperada da API de preços de mercado para o EAN {Ean}.", ean);
            return Result<ConsultaExternaPrecos>.Failure("A API de preços de mercado retornou um formato inesperado.");
        }

        var precos = MapearPrecos(payload, cidade);
        return Result<ConsultaExternaPrecos>.Success(new ConsultaExternaPrecos(precos, OrigemCache: false));
    }

    /// <summary>
    /// Converte todas as lojas do retorno da API (<c>stores[]</c>) em itens
    /// de resposta, sem filtro de rede, ordenadas pelo menor preço. Lojas sem
    /// preço vêm no final, marcadas como indisponíveis.
    /// </summary>
    private static IReadOnlyList<PrecoMercadoRedeResponse> MapearPrecos(
        DataMarketProductResponse? payload, string? cidade)
    {
        var lojas = payload?.Stores ?? [];
        return lojas
            .Select(l => new PrecoMercadoRedeResponse(
                l.StoreName ?? "Loja desconhecida",
                l.City ?? cidade,
                l.Price,
                l.Price is not null,
                l.Price is null ? "Preço indisponível nesta loja." : null))
            .OrderBy(p => p.Preco ?? decimal.MaxValue)
            .ToArray();
    }

    // Contrato real da resposta da Data Market (verificado em 16/09/2026):
    // { "ean", "product", "brand", "summary": {...}, "stores": [...] }
    private sealed record DataMarketProductResponse(
        [property: JsonPropertyName("stores")] IReadOnlyList<DataMarketStore>? Stores);

    private sealed record DataMarketStore(
        [property: JsonPropertyName("store_name")] string? StoreName,
        [property: JsonPropertyName("city")] string? City,
        [property: JsonPropertyName("state")] string? State,
        [property: JsonPropertyName("price")] decimal? Price,
        [property: JsonPropertyName("in_stock")] bool InStock);
}
