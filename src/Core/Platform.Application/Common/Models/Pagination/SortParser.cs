namespace Platform.Application.Common.Models.Pagination;

/// <summary>
/// SQL injection'a karşı whitelist zorunlu.
/// </summary>
public static class SortParser
{
    /// <summary>
    /// "createdAt desc,title asc" → List&lt;SortDescriptor&gt;
    /// Whitelist'te olmayan alanlar sessizce atlanır.
    /// </summary>
    public static IList<SortDescriptor> Parse(string? sort, IReadOnlySet<string> allowedFields)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return [];

        return sort
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token =>
            {
                var parts = token.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var field = parts[0].Trim();
                var dir   = parts.Length > 1 &&
                            parts[1].Trim().Equals("desc", StringComparison.OrdinalIgnoreCase)
                    ? SortDirection.Desc
                    : SortDirection.Asc;
                return (field, dir);
            })
            .Where(t => allowedFields.Contains(t.field, StringComparer.OrdinalIgnoreCase))
            .Select(t => new SortDescriptor(t.field, t.dir))
            .ToList();
    }

    /// <summary>
    /// SortDescriptor listesini EF Core IOrderedQueryable'a dönüştürür.
    /// </summary>
    public static IOrderedQueryable<T> ApplySort<T>(
        IQueryable<T>              query,
        IList<SortDescriptor>      descriptors,
        string                     fallbackField,
        Dictionary<string, System.Linq.Expressions.Expression<Func<T, object>>> fieldMap)
    {
        IOrderedQueryable<T>? ordered = null;

        foreach (var desc in descriptors)
        {
            if (!fieldMap.TryGetValue(desc.Field, out var expr)) continue;

            ordered = ordered is null
                ? desc.Direction == SortDirection.Asc
                    ? query.OrderBy(expr)
                    : query.OrderByDescending(expr)
                : desc.Direction == SortDirection.Asc
                    ? ordered.ThenBy(expr)
                    : ordered.ThenByDescending(expr);
        }

        if (ordered is null && fieldMap.TryGetValue(fallbackField, out var fallback))
            ordered = query.OrderByDescending(fallback);

        return ordered ?? query.OrderByDescending(x => x);
    }
}
