using Platform.Application.Common.Models.Pagination;

namespace Platform.Application.Features.Orders.Constants;

/// <summary>
/// Cache keys for Order feature.
/// </summary>
public static class OrderCacheKeys
{
    public static readonly TimeSpan GetByIdTtl = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan GetByUserTtl = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan GetAllTtl = TimeSpan.FromMinutes(5);

    public static string GetAll(PageRequest pr)
    {
        var search = string.IsNullOrWhiteSpace(pr.Search) ? "_" : pr.Search.ToLower().Trim();
        var sort = string.IsNullOrWhiteSpace(pr.Sort) ? "_" : pr.Sort.ToLower().Trim();
        return $"orders:list:p{pr.Page}:s{pr.PageSize}:sort:{sort}:q:{search}";
    }

    public static string GetById(Guid id) => $"orders:id:{id}";
    public static string ByUser(Guid userId, int page, int pageSize) =>
        $"orders:user:{userId}:p{page}:s{pageSize}";
    public static string InvalidateUser(Guid userId) => $"orders:user:{userId}:*";
    public static string Invalidate() => "orders:*";
}
