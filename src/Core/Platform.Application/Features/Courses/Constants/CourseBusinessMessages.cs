namespace Platform.Application.Features.Courses.Constants;

/// <summary>
/// Course business rule messages.
/// </summary>
public static class CourseBusinessMessages
{
    // Not Found
    public const string NotFound = "Course not found.";
    public const string NotFoundById = "Course with the specified ID was not found.";
    public const string NotFoundBySlug = "Course with the specified slug was not found.";

    // Status Errors
    public const string AlreadyPublished = "Course is already published.";
    public const string AlreadyArchived = "Course is already archived.";
    public const string NotDraft = "Only draft courses can be published.";
    public const string NotPublished = "Only published courses can be archived.";
    public const string NotArchived = "Only archived courses can be restored to draft.";
    public const string CannotDeletePublished = "Cannot delete a published course. Archive it first.";

    // Slug Errors
    public const string SlugExists = "A course with this slug already exists.";
    public const string SlugExistsForOtherCourse = "Slug is already used by another course.";

    // Category Errors
    public const string CategoryNotFound = "The specified category does not exist.";
    public const string CategoryNotActive = "The specified category is not active.";

    // Instructor Errors
    public const string InstructorNotFound = "The specified instructor does not exist.";
    public const string InstructorNotInstructorRole = "The specified user is not an instructor.";

    // Purchase Errors
    public const string NotPurchasable = "This course is not available for purchase.";
    public const string CourseIsDraft = "This course is still in draft mode.";
    public const string CourseIsArchived = "This course has been archived.";

    // Success Messages
    public const string CreatedSuccessfully = "Course created successfully.";
    public const string UpdatedSuccessfully = "Course updated successfully.";
    public const string DeletedSuccessfully = "Course deleted successfully.";
    public const string PublishedSuccessfully = "Course published successfully.";
    public const string ArchivedSuccessfully = "Course archived successfully.";
    public const string RestoredSuccessfully = "Course restored to draft successfully.";
}
