namespace Platform.Application.Features.Coupons.Constants;

/// <summary>
/// Cache keys for Coupon feature.
/// </summary>
public static class CouponCacheKeys
{
    public static string GetAll(int page, int pageSize) => $"coupons:p{page}:s{pageSize}";
    public static string GetById(Guid id) => $"coupons:id:{id}";
    public static string ByCode(string code) => $"coupons:code:{code.ToLowerInvariant()}";
    public static string Invalidate() => "coupons:*";
}
