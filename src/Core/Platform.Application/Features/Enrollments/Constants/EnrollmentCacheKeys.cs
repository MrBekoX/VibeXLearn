using Platform.Application.Common.Models.Pagination;

namespace Platform.Application.Features.Enrollments.Constants;

/// <summary>
/// Cache keys for Enrollment feature.
/// </summary>
public static class EnrollmentCacheKeys
{
    public static string GetAll(PageRequest pr)
    {
        var search = string.IsNullOrWhiteSpace(pr.Search) ? "_" : pr.Search.ToLower().Trim();
        var sort = string.IsNullOrWhiteSpace(pr.Sort) ? "_" : pr.Sort.ToLower().Trim();
        return $"enrollments:list:p{pr.Page}:s{pr.PageSize}:sort:{sort}:q:{search}";
    }

    public static string GetById(Guid id) => $"enrollments:id:{id}";
    public static string ByUser(Guid userId, int page, int pageSize) =>
        $"enrollments:user:{userId}:p{page}:s{pageSize}";
    public static string ByCourse(Guid courseId, int page, int pageSize) =>
        $"enrollments:course:{courseId}:p{page}:s{pageSize}";
    public static string ByUserAndCourse(Guid userId, Guid courseId) =>
        $"enrollments:user:{userId}:course:{courseId}";
    public static string InvalidateUser(Guid userId) => $"enrollments:user:{userId}:*";
    public static string ByCoursePattern(Guid courseId) => $"enrollments:course:{courseId}:*";
    public static string Invalidate() => "enrollments:*";
}
