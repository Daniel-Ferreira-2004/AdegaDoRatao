namespace AdegaDoRatao.Infrastructure.ExternalServices;

/// <summary>
/// Configurações do serviço de geocodificação, lidas da seção
/// <c>Geocoding</c>. O padrão é o Nominatim (OpenStreetMap), gratuito e
/// SEM chave de API — por isso não há segredo obrigatório aqui.
///
/// Se no futuro migrar para Google Maps Geocoding, a chave deve ficar em
/// User Secrets (<c>dotnet user-secrets set "Geocoding:ApiKey" "..."</c>)
/// ou na variável de ambiente <c>Geocoding__ApiKey</c> — mesmo padrão do
/// <c>Jwt:Secret</c> e do <c>DataMarket:ApiKey</c>. NUNCA no appsettings.json.
/// </summary>
public sealed class GeocodingOptions
{
    public const string SectionName = "Geocoding";

    /// <summary>
    /// Provedor de geocodificação: "Nominatim" (padrão, gratuito) ou
    /// "Google" (Google Maps Geocoding API — exige <see cref="ApiKey"/>).
    /// </summary>
    public string Provider { get; init; } = "Nominatim";

    /// <summary>URL base do serviço. Padrão: Nominatim público.</summary>
    public string BaseUrl { get; init; } = "https://nominatim.openstreetmap.org";

    /// <summary>URL base do Google Maps Geocoding (usada quando Provider = "Google").</summary>
    public string GoogleBaseUrl { get; init; } = "https://maps.googleapis.com";

    /// <summary>
    /// User-Agent enviado nas requisições. O Nominatim EXIGE um User-Agent
    /// identificando a aplicação (política de uso do OSM).
    /// </summary>
    public string UserAgent { get; init; } = "AdegaDoRatao/1.0 (contato@adegadoratao.local)";

    /// <summary>Chave de API — vazia no Nominatim; usada apenas se migrar para provedor pago.</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>Timeout de cada chamada HTTP, em segundos.</summary>
    public int TimeoutSeconds { get; init; } = 10;
}
