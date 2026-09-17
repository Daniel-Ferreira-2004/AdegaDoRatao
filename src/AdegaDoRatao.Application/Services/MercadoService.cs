using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using AdegaDoRatao.Domain.Entities;
using AdegaDoRatao.Domain.Interfaces;
using FluentValidation;

namespace AdegaDoRatao.Application.Services;

/// <summary>
/// Caso de uso: filtro de "mercados próximos".
///
/// Carrega os mercados ativos do banco e calcula a distância de cada um
/// até o ponto informado usando Haversine em memória (<see cref="GeoDistance"/>).
/// Funciona sem PostGIS; se o catálogo crescer muito, o filtro pode ser
/// movido para o banco (ver docs/18-MERCADOS-PROXIMOS.md).
/// </summary>
public sealed class MercadoService(
    IMarketRepository markets,
    IGeocodingService geocoding,
    IValidator<BuscarMercadosProximosQuery> validator) : IMercadoService
{
    public async Task<IReadOnlyList<MercadoProximoResponse>> BuscarProximosAsync(
        BuscarMercadosProximosQuery query, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            throw new UseCaseException(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));
        }

        var mercados = await markets.ListarAtivosAsync(cancellationToken);

        return mercados
            .Select(m => new MercadoProximoResponse(
                m.Id,
                m.Name,
                m.Rede,
                m.Address,
                m.City,
                m.State,
                DistanciaKm: Math.Round(
                    GeoDistance.CalcularKm(query.Latitude, query.Longitude, m.Latitude, m.Longitude), 2)))
            .Where(x => x.DistanciaKm <= query.RaioKm)
            .OrderBy(x => x.DistanciaKm)
            .ToArray();
    }

    public async Task<IReadOnlyList<MercadoProximoResponse>> BuscarProximosPorEnderecoAsync(
        string endereco, double raioKm = 5.0, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(endereco))
        {
            throw new UseCaseException("Informe um endereço ou CEP para a busca.");
        }

        var resultado = await geocoding.GeocodificarAsync(endereco.Trim(), cancellationToken);
        if (!resultado.Succeeded || resultado.Value is null)
        {
            throw new UseCaseException(
                resultado.Error ?? "Não foi possível localizar o endereço informado.");
        }

        return await BuscarProximosAsync(
            new BuscarMercadosProximosQuery(resultado.Value.Latitude, resultado.Value.Longitude, raioKm),
            cancellationToken);
    }
}
