namespace Platform.Application.Common.Models.Pagination;

/// <summary>
/// Sayfalanmış sonuç modeli.
/// </summary>
public sealed record PagedResult<T>(
    IList<T> Items,
    int      TotalCount,
    int      Page,
    int      PageSize)
{
    public int  TotalPages   => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasNextPage  => Page < TotalPages;
    public bool HasPrevPage  => Page > 1;
    public bool IsEmpty      => Items.Count == 0;
    public int  CurrentCount => Items.Count;

    /// <summary>
    /// Projection dönüşümü.
    /// </summary>
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> mapper)
        => new(Items.Select(mapper).ToList(), TotalCount, Page, PageSize);

    /// <summary>HTTP response header'larına yazılacak metadata.</summary>
    public PaginationMetadata ToMetadata() => new(TotalCount, TotalPages, Page, PageSize, HasNextPage, HasPrevPage);
}
