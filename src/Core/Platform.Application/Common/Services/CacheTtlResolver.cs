using Microsoft.Extensions.Options;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Models.Cache;

namespace Platform.Application.Common.Services;

/// <summary>
/// Central TTL mapping based on normalized cache key prefixes.
/// </summary>
public sealed class CacheTtlResolver(IOptions<CacheSettings> options) : ICacheTtlResolver
{
    private readonly CacheSettings _settings = options.Value;

    public TimeSpan ResolveL2Duration(ICacheableQuery query)
    {
        var key = query.CacheKey;
        if (string.IsNullOrWhiteSpace(key))
            return _settings.DefaultL2Duration;

        return key switch
        {
            var k when k.StartsWith("categories:tree", StringComparison.Ordinal) => _settings.CategoryTreeL2,
            var k when k.StartsWith("categories:list", StringComparison.Ordinal) => _settings.CategoryListL2,
            var k when k.StartsWith("categories:id", StringComparison.Ordinal) => _settings.CategoryByIdL2,
            var k when k.StartsWith("categories:slug", StringComparison.Ordinal) => _settings.CategoryBySlugL2,

            var k when k.StartsWith("courses:list", StringComparison.Ordinal) => _settings.CourseListL2,
            var k when k.StartsWith("courses:id", StringComparison.Ordinal) => _settings.CourseByIdL2,
            var k when k.StartsWith("courses:slug", StringComparison.Ordinal) => _settings.CourseBySlugL2,
            var k when k.StartsWith("courses:instructor", StringComparison.Ordinal) => _settings.CourseByInstructorL2,

            var k when k.StartsWith("lessons:course", StringComparison.Ordinal) => _settings.LessonByCourseL2,
            var k when k.StartsWith("lessons:id", StringComparison.Ordinal) => _settings.LessonByIdL2,

            var k when k.StartsWith("badges:p", StringComparison.Ordinal) => _settings.BadgeListL2,
            var k when k.StartsWith("badges:id", StringComparison.Ordinal) => _settings.BadgeByIdL2,

            var k when k.StartsWith("coupons:p", StringComparison.Ordinal) => _settings.CouponListL2,
            var k when k.StartsWith("coupons:id", StringComparison.Ordinal) => _settings.CouponByIdL2,
            var k when k.StartsWith("coupons:code", StringComparison.Ordinal) => _settings.CouponByCodeL2,

            var k when k.StartsWith("enrollments:list", StringComparison.Ordinal) => _settings.EnrollmentByUserL2,
            var k when k.StartsWith("enrollments:id", StringComparison.Ordinal) => _settings.EnrollmentByUserL2,
            var k when k.StartsWith("enrollments:user", StringComparison.Ordinal) => _settings.EnrollmentByUserL2,
            var k when k.StartsWith("enrollments:course", StringComparison.Ordinal) => _settings.EnrollmentByCourseL2,

            var k when k.StartsWith("submissions:student", StringComparison.Ordinal) => _settings.SubmissionByStudentL2,
            var k when k.StartsWith("submissions:id", StringComparison.Ordinal) => _settings.SubmissionByStudentL2,
            var k when k.StartsWith("submissions:lesson", StringComparison.Ordinal) => _settings.SubmissionByStudentL2,

            var k when k.StartsWith("certificates:user", StringComparison.Ordinal) => _settings.CertificateByUserL2,
            var k when k.StartsWith("certificates:id", StringComparison.Ordinal) => _settings.CertificateByUserL2,

            var k when k.StartsWith("livesessions:id", StringComparison.Ordinal) => _settings.LiveSessionByIdL2,
            var k when k.StartsWith("livesessions:lesson", StringComparison.Ordinal) => _settings.LiveSessionByIdL2,
            var k when k.StartsWith("livesessions:upcoming", StringComparison.Ordinal) => _settings.LiveSessionByIdL2,

            _ => _settings.DefaultL2Duration
        };
    }
}
