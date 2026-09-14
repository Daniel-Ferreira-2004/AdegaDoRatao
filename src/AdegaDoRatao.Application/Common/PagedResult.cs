namespace AdegaDoRatao.Application.Common;

/// <summary>
/// Envelope comum para listas paginadas. Clientes mobile recebem somente a
/// página solicitada e podem saber se há mais conteúdo sem baixar tudo.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
}
