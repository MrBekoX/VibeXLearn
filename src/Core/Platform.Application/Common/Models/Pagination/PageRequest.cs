namespace Platform.Application.Common.Models.Pagination;

/// <summary>
/// Tüm GetAll Query'lerin composition yoluyla kullandığı sayfalama parametresi.
/// SKILL: input-validation-limits + pagination-limits
/// </summary>
public sealed record PageRequest
{
    private const int MaxSearchLength = 200;
    private const int MaxSortLength = 500;

    public int Page { get; init; } = 1;

    private int _pageSize = 20;
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value;
    }

    private string? _sort;
    /// <summary>
    /// Virgülle ayrılmış sıralama ifadeleri.
    /// Format: "fieldName asc|desc"
    /// </summary>
    public string? Sort
    {
        get => _sort;
        init => _sort = value?.Length > MaxSortLength ? value[..MaxSortLength] : value;
    }

    private string? _search;
    /// <summary>
    /// Genel arama terimi. Max 200 karakter.
    /// </summary>
    public string? Search
    {
        get => _search;
        init => _search = value?.Length > MaxSearchLength ? value[..MaxSearchLength] : value;
    }

    public static readonly PageRequest Default = new();

    /// <summary>
    /// Normalizes pagination parameters with enforced limits.
    /// - Page: minimum 1
    /// - PageSize: 1-100 range
    /// - Search: trimmed and limited to 200 chars
    /// - Sort: limited to 500 chars
    /// </summary>
    public PageRequest Normalize() => this with
    {
        Page     = Page < 1 ? 1 : Page,
        PageSize = PageSize < 1 ? 20 : PageSize > 100 ? 100 : PageSize,
        Search   = Search?.Trim(),
        Sort     = Sort?.Trim()
    };
}
