namespace AdegaDoRatao.Application.DTOs;

/// <summary>
/// Consulta de mercados próximos a um ponto. RaioKm é opcional e tem
/// padrão de 5 km (limites validados por FluentValidation: 0,5 a 20 km).
/// </summary>
public sealed record BuscarMercadosProximosQuery(
    double Latitude,
    double Longitude,
    double RaioKm = 5.0);

/// <summary>
/// Mercado encontrado dentro do raio, já com a distância calculada
/// (em km, arredondada em 2 casas) para exibição/ordenação na UI.
/// </summary>
public sealed record MercadoProximoResponse(
    Guid Id,
    string Nome,
    string Rede,
    string Endereco,
    string? Cidade,
    string? Estado,
    double DistanciaKm);
