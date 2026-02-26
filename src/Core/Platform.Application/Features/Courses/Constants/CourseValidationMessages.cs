namespace Platform.Application.Features.Courses.Constants;

/// <summary>
/// Course validation messages.
/// </summary>
public static class CourseValidationMessages
{
    // Title
    public const string TitleRequired = "Course title is required.";
    public const string TitleMaxLength = "Course title cannot exceed 200 characters.";
    public const string TitleMinLength = "Course title must be at least 3 characters.";

    // Slug
    public const string SlugRequired = "Course slug is required.";
    public const string SlugMaxLength = "Course slug cannot exceed 200 characters.";
    public const string SlugInvalidFormat = "Slug must contain only lowercase letters, numbers, and hyphens.";

    // Price
    public const string PriceRequired = "Course price is required.";
    public const string PricePositive = "Course price must be greater than zero.";
    public const string PriceMax = "Course price cannot exceed 99,999.99.";

    // Description
    public const string DescriptionMaxLength = "Course description cannot exceed 5000 characters.";

    // Thumbnail
    public const string ThumbnailInvalidUrl = "Thumbnail must be a valid URL.";
    public const string ThumbnailMaxLength = "Thumbnail URL cannot exceed 500 characters.";

    // Level
    public const string LevelRequired = "Course level is required.";
    public const string LevelInvalid = "Invalid course level. Must be Beginner, Intermediate, or Advanced.";

    // Category
    public const string CategoryIdRequired = "Category ID is required.";
    public const string CategoryIdEmpty = "Category ID cannot be empty.";
    public const string CategoryRequired = "Category ID is required.";

    // Instructor
    public const string InstructorIdRequired = "Instructor ID is required.";
    public const string InstructorIdEmpty = "Instructor ID cannot be empty.";
    public const string InstructorRequired = "Instructor ID is required.";

    // Course ID
    public const string CourseIdRequired = "Course ID is required.";
    public const string CourseIdEmpty = "Course ID cannot be empty.";
}
