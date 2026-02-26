namespace Platform.Application.Features.Lessons.Constants;

public static class LessonValidationMessages
{
    public const string LessonIdRequired = "Lesson ID is required.";
    public const string CourseIdRequired = "Course ID is required.";
    public const string TitleRequired = "Lesson title is required.";
    public const string TitleMaxLength = "Lesson title cannot exceed 200 characters.";
    public const string OrderNonNegative = "Order must be non-negative.";
    public const string VideoUrlMaxLength = "Video URL must not exceed 500 characters.";
    public const string LessonsRequired = "Lessons list is required and must not be empty.";
}
