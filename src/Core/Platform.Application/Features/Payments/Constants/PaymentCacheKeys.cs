using Platform.Application.Common.Models.Pagination;

namespace Platform.Application.Features.Payments.Constants;

/// <summary>
/// Cache keys for Payment feature.
/// </summary>
public static class PaymentCacheKeys
{
    public static readonly TimeSpan GetAllTtl = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan GetByIdTtl = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan GetByOrderTtl = TimeSpan.FromMinutes(10);

    public static string GetAll(PageRequest pr)
    {
        var search = string.IsNullOrWhiteSpace(pr.Search) ? "_" : pr.Search.ToLower().Trim();
        var sort = string.IsNullOrWhiteSpace(pr.Sort) ? "_" : pr.Sort.ToLower().Trim();
        return $"payments:list:p{pr.Page}:s{pr.PageSize}:sort:{sort}:q:{search}";
    }

    public static string GetById(Guid id) => $"payments:id:{id}";
    public static string ByOrder(Guid orderId) => $"payments:order:{orderId}";
    public static string ByConversationId(string conversationId) => $"payments:conv:{conversationId}";
    public static string Invalidate() => "payments:*";
}
