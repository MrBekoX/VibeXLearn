namespace Platform.Application.Common.Models.Pagination;

/// <summary>
/// Cursor-based pagination altyapısı — sonsuz scroll endpoint'leri için.
/// </summary>
public sealed record CursorRequest
{
    /// <summary>
    /// Bir önceki sayfanın son öğesini işaret eden opak imleç.
    /// </summary>
    public string? After    { get; init; }
    public string? Before   { get; init; }
    public int     Limit    { get; init; } = 20;

    public int SafeLimit => Limit is < 1 or > 100 ? 20 : Limit;
}

public sealed record CursorPagedResult<T>(
    IList<T> Items,
    string?  NextCursor,
    string?  PrevCursor,
    bool     HasMore)
{
    public bool IsEmpty => Items.Count == 0;
}
