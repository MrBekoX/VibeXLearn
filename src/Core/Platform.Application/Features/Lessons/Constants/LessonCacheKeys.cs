namespace Platform.Application.Features.Lessons.Constants;

public static class LessonCacheKeys
{
    public static string GetById(Guid id) => $"lessons:id:{id}";
    public static string ByCourse(Guid courseId) => $"lessons:course:{courseId}";
    public static string Invalidate() => "lessons:*";
}
