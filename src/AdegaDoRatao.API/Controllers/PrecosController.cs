using AdegaDoRatao.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdegaDoRatao.API.Controllers;

/// <summary>
/// Disparo manual do agente de preços (coletores Tenda, Atacadão, Shibata
/// e Sonda). Útil para testes e para não depender do job diário das 06h.
/// Restrito a ADMIN — a coleta é pesada (Shibata/Sonda usam navegador).
/// </summary>
[ApiController]
[Route("api/precos")]
[Authorize(Roles = "ADMIN")]
public sealed class PrecosController(IAtualizadorPrecosRedesService atualizador) : ControllerBase
{
    /// <summary>
    /// Coleta os preços de TODOS os produtos ativos com EAN em todas as
    /// redes. Pode demorar vários minutos (coletores via navegador).
    /// Retorna a quantidade de EANs processados.
    /// </summary>
    [HttpPost("atualizar")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> AtualizarTodos(CancellationToken ct)
    {
        var total = await atualizador.AtualizarTodosAsync(ct);
        return Ok(new { eansProcessados = total });
    }

    /// <summary>
    /// Coleta os preços de um único EAN em todas as redes. Ideal para
    /// testar o agente ou atualizar um produto pontual.
    /// </summary>
    [HttpPost("atualizar/{ean}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AtualizarPorEan(string ean, CancellationToken ct)
    {
        await atualizador.AtualizarPorEanAsync(ean, ct);
        return NoContent();
    }
}
