using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdegaDoRatao.API.Controllers;

[ApiController]
[Route("api/mercados")]
[Authorize]
public sealed class MarketsController(IMercadoService service) : ControllerBase
{
    /// <summary>
    /// Lista os mercados próximos, ordenados do mais próximo ao mais distante.
    ///
    /// Dois modos de uso (informe UM deles):
    ///  - Por coordenadas: ?latitude=-23.54&amp;longitude=-46.31&amp;raioKm=5
    ///  - Por endereço/CEP: ?endereco=Rua+Exemplo,+Suzano&amp;raioKm=5
    ///    (o endereço é geocodificado internamente via Nominatim/OpenStreetMap)
    ///
    /// raioKm é opcional (padrão 5 km; mínimo 0,5; máximo 20).
    /// Endereço inválido/não encontrado retorna 400 com mensagem clara.
    /// </summary>
    [HttpGet("proximos")]
    [ProducesResponseType(typeof(IReadOnlyList<MercadoProximoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IReadOnlyList<MercadoProximoResponse>> Proximos(
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        [FromQuery] string? endereco,
        [FromQuery] double raioKm = 5.0,
        CancellationToken cancellationToken = default)
    {
        if (latitude is not null && longitude is not null)
        {
            return await service.BuscarProximosAsync(
                new BuscarMercadosProximosQuery(latitude.Value, longitude.Value, raioKm), cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(endereco))
        {
            return await service.BuscarProximosPorEnderecoAsync(endereco, raioKm, cancellationToken);
        }

        throw new UseCaseException(
            "Informe latitude e longitude, ou um endereço/CEP, para buscar os mercados próximos.");
    }
}
