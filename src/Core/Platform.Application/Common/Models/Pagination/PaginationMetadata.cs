namespace Platform.Application.Common.Models.Pagination;

/// <summary>
/// X-Pagination header'ında JSON olarak gönderilir.
/// </summary>
public sealed record PaginationMetadata(
    int  TotalCount,
    int  TotalPages,
    int  CurrentPage,
    int  PageSize,
    bool HasNextPage,
    bool HasPrevPage);
