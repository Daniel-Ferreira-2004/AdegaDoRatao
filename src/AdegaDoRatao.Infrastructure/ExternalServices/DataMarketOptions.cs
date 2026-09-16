namespace AdegaDoRatao.Infrastructure.ExternalServices;

/// <summary>
/// Configurações da integração com a Cnova Tech Data Market, lidas da
/// seção <c>DataMarket</c>. A API Key NUNCA deve ficar no appsettings.json
/// versionado: use User Secrets no desenvolvimento
/// (<c>dotnet user-secrets set "DataMarket:ApiKey" "..."</c>) e a variável
/// de ambiente <c>DataMarket__ApiKey</c> em produção — mesmo padrão do
/// <c>Jwt:Secret</c>.
/// </summary>
public sealed class DataMarketOptions
{
    public const string SectionName = "DataMarket";

    /// <summary>URL base da API. Padrão: https://datamarket.cnovatech.com.br</summary>
    public string BaseUrl { get; init; } = "https://datamarket.cnovatech.com.br";

    /// <summary>Chave de API (header X-API-Key). Vazia até ser configurada via secrets/ambiente.</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>Timeout de cada chamada HTTP, em segundos.</summary>
    public int TimeoutSeconds { get; init; } = 10;

    /// <summary>
    /// Tempo de vida do cache em memória, em horas. Padrão 6h — essencial
    /// para não estourar o limite de 50 consultas/mês do plano Free.
    /// </summary>
    public int CacheHours { get; init; } = 6;
}
