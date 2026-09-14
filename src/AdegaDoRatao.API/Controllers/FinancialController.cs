using AdegaDoRatao.Application.Common;
using AdegaDoRatao.Application.DTOs;
using AdegaDoRatao.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdegaDoRatao.API.Controllers;

/// <summary>
/// Endpoints do módulo financeiro (RN24–RN27).
/// Categorias, lançamentos manuais, estorno e fluxo de caixa.
/// </summary>
[ApiController]
[Route("api/financial")]
[Authorize]
public sealed class FinancialController(IFinancialService service) : ControllerBase
{
    // ========================================================================
    // Categorias financeiras
    // ========================================================================

    [HttpPost("categories")]
    [Authorize(Policy = "financial.write")]
    public async Task<ActionResult<FinancialCategoryResponse>> CreateCategory(
        CreateFinancialCategoryRequest request, CancellationToken ct)
    {
        var category = await service.CreateCategoryAsync(request, ct);
        return CreatedAtAction(nameof(ListCategories), new { id = category.Id }, category);
    }

    [HttpGet("categories")]
    [Authorize(Policy = "financial.read")]
    public Task<IReadOnlyList<FinancialCategoryResponse>> ListCategories(CancellationToken ct)
        => service.ListCategoriesAsync(ct);

    [HttpPatch("categories/{id:guid}/active")]
    [Authorize(Policy = "financial.write")]
    public async Task<IActionResult> SetCategoryActive(Guid id, [FromBody] ActiveStatusRequest request, CancellationToken ct)
    {
        await service.SetCategoryActiveAsync(id, request.IsActive, ct);
        return NoContent();
    }

    // ========================================================================
    // Lançamentos financeiros
    // ========================================================================

    [HttpPost("transactions")]
    [Authorize(Policy = "financial.write")]
    public async Task<ActionResult<FinancialTransactionResponse>> CreateTransaction(
        CreateFinancialTransactionRequest request, CancellationToken ct)
    {
        var transaction = await service.CreateTransactionAsync(request, ct);
        return CreatedAtAction(nameof(SearchTransactions), new { id = transaction.Id }, transaction);
    }

    [HttpGet("transactions")]
    [Authorize(Policy = "financial.read")]
    public Task<PagedResult<FinancialTransactionResponse>> SearchTransactions(
        [FromQuery] FinancialTransactionQuery query, CancellationToken ct)
        => service.SearchTransactionsAsync(query, ct);

    [HttpPost("transactions/{id:guid}/estornar")]
    [Authorize(Policy = "financial.write")]
    public async Task<IActionResult> Estornar(Guid id, CancellationToken ct)
    {
        await service.EstornarAsync(id, ct);
        return NoContent();
    }

    // ========================================================================
    // Fluxo de caixa
    // ========================================================================

    [HttpGet("cashflow")]
    [Authorize(Policy = "financial.read")]
    public Task<CashFlowResponse> GetCashFlow(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
        => service.GetCashFlowAsync(from, to, ct);
}
