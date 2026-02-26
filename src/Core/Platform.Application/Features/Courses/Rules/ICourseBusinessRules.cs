using Platform.Application.Common.Rules;

namespace Platform.Application.Features.Courses.Rules;

/// <summary>
/// Course business rules interface.
/// </summary>
public interface ICourseBusinessRules
{
    /// <summary>
    /// Rule: Category must exist in the system.
    /// </summary>
    IBusinessRule CategoryMustExist(Guid categoryId);

    /// <summary>
    /// Rule: Instructor must exist and have instructor role.
    /// </summary>
    IBusinessRule InstructorMustExist(Guid instructorId);

    /// <summary>
    /// Rule: Course slug must be unique.
    /// </summary>
    IBusinessRule SlugMustBeUnique(string slug);

    /// <summary>
    /// Rule: Course slug must be unique (excluding current course).
    /// </summary>
    IBusinessRule SlugMustBeUniqueForUpdate(string slug, Guid courseId);

    /// <summary>
    /// Rule: Course must exist.
    /// </summary>
    IBusinessRule CourseMustExist(Guid courseId);

    /// <summary>
    /// Rule: Course must be in Draft status.
    /// </summary>
    IBusinessRule CourseMustBeDraft(Guid courseId);

    /// <summary>
    /// Rule: Course must be in Published status.
    /// </summary>
    IBusinessRule CourseMustBePublished(Guid courseId);

    /// <summary>
    /// Rule: Course must be in Archived status.
    /// </summary>
    IBusinessRule CourseMustBeArchived(Guid courseId);

    /// <summary>
    /// Rule: Course must be purchasable (Published and not deleted).
    /// </summary>
    IBusinessRule CourseMustBePurchasable(Guid courseId);

    /// <summary>
    /// Rule: Course must not be published (for delete operation).
    /// </summary>
    IBusinessRule CourseMustNotBePublished(Guid courseId);

    /// <summary>
    /// Rule: Price must be within allowed range.
    /// </summary>
    IBusinessRule PriceMustBeValid(decimal price);
}
