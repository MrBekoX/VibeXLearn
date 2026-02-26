using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Courses.Constants;
using Platform.Application.Common.Interfaces;
using Platform.Domain.Entities;
using Platform.Domain.Enums;

namespace Platform.Application.Features.Courses.Rules;

/// <summary>
/// Course business rules implementation.
/// </summary>
public sealed class CourseBusinessRules(
    IReadRepository<Course> courseRepo,
    IReadRepository<Category> categoryRepo,
    IIdentityAccessService identityAccess) : ICourseBusinessRules
{
    public IBusinessRule CategoryMustExist(Guid categoryId)
        => new BusinessRule(
            "COURSE_CATEGORY_NOT_FOUND",
            CourseBusinessMessages.CategoryNotFound,
            async ct => await categoryRepo.AnyAsync(c => c.Id == categoryId, ct)
                ? Result.Success()
                : Result.Fail(CourseBusinessMessages.CategoryNotFound));

    public IBusinessRule InstructorMustExist(Guid instructorId)
        => new BusinessRule(
            "COURSE_INSTRUCTOR_NOT_FOUND",
            CourseBusinessMessages.InstructorNotFound,
            async ct =>
            {
                if (!await identityAccess.UserExistsAsync(instructorId, ct))
                    return Result.Fail(CourseBusinessMessages.InstructorNotFound);

                if (!await identityAccess.UserInRoleAsync(instructorId, "Instructor", ct))
                    return Result.Fail(CourseBusinessMessages.InstructorNotInstructorRole);

                return Result.Success();
            });

    public IBusinessRule SlugMustBeUnique(string slug)
        => new BusinessRule(
            "COURSE_SLUG_EXISTS",
            CourseBusinessMessages.SlugExists,
            async ct => await courseRepo.AnyAsync(c => c.Slug == slug, ct)
                ? Result.Fail(CourseBusinessMessages.SlugExists)
                : Result.Success());

    public IBusinessRule SlugMustBeUniqueForUpdate(string slug, Guid courseId)
        => new BusinessRule(
            "COURSE_SLUG_EXISTS",
            CourseBusinessMessages.SlugExistsForOtherCourse,
            async ct => await courseRepo.AnyAsync(c => c.Slug == slug && c.Id != courseId, ct)
                ? Result.Fail(CourseBusinessMessages.SlugExistsForOtherCourse)
                : Result.Success());

    public IBusinessRule CourseMustExist(Guid courseId)
        => new BusinessRule(
            "COURSE_NOT_FOUND",
            CourseBusinessMessages.NotFoundById,
            async ct => await courseRepo.AnyAsync(c => c.Id == courseId, ct)
                ? Result.Success()
                : Result.Fail(CourseBusinessMessages.NotFoundById));

    public IBusinessRule CourseMustBeDraft(Guid courseId)
        => new BusinessRule(
            "COURSE_NOT_DRAFT",
            CourseBusinessMessages.NotDraft,
            async ct =>
            {
                var course = await courseRepo.GetAsync(c => c.Id == courseId, ct);
                if (course is null)
                    return Result.Fail(CourseBusinessMessages.NotFoundById);
                return course.Status == CourseStatus.Draft
                    ? Result.Success()
                    : Result.Fail(CourseBusinessMessages.NotDraft);
            });

    public IBusinessRule CourseMustBePublished(Guid courseId)
        => new BusinessRule(
            "COURSE_NOT_PUBLISHED",
            CourseBusinessMessages.NotPublished,
            async ct =>
            {
                var course = await courseRepo.GetAsync(c => c.Id == courseId, ct);
                if (course is null)
                    return Result.Fail(CourseBusinessMessages.NotFoundById);
                return course.Status == CourseStatus.Published
                    ? Result.Success()
                    : Result.Fail(CourseBusinessMessages.NotPublished);
            });

    public IBusinessRule CourseMustBeArchived(Guid courseId)
        => new BusinessRule(
            "COURSE_NOT_ARCHIVED",
            CourseBusinessMessages.NotArchived,
            async ct =>
            {
                var course = await courseRepo.GetAsync(c => c.Id == courseId, ct);
                if (course is null)
                    return Result.Fail(CourseBusinessMessages.NotFoundById);
                return course.Status == CourseStatus.Archived
                    ? Result.Success()
                    : Result.Fail(CourseBusinessMessages.NotArchived);
            });

    public IBusinessRule CourseMustBePurchasable(Guid courseId)
        => new BusinessRule(
            "COURSE_NOT_PURCHASABLE",
            CourseBusinessMessages.NotPurchasable,
            async ct =>
            {
                var course = await courseRepo.GetAsync(c => c.Id == courseId, ct);
                if (course is null)
                    return Result.Fail(CourseBusinessMessages.NotFoundById);
                return course.CanBePurchased
                    ? Result.Success()
                    : Result.Fail(CourseBusinessMessages.NotPurchasable);
            });

    public IBusinessRule CourseMustNotBePublished(Guid courseId)
        => new BusinessRule(
            "COURSE_CANNOT_DELETE_PUBLISHED",
            CourseBusinessMessages.CannotDeletePublished,
            async ct =>
            {
                var course = await courseRepo.GetAsync(c => c.Id == courseId, ct);
                if (course is null)
                    return Result.Fail(CourseBusinessMessages.NotFoundById);
                return course.Status != CourseStatus.Published
                    ? Result.Success()
                    : Result.Fail(CourseBusinessMessages.CannotDeletePublished);
            });

    public IBusinessRule PriceMustBeValid(decimal price)
        => new BusinessRule(
            "COURSE_PRICE_INVALID",
            CourseValidationMessages.PricePositive,
            ct =>
            {
                if (price <= 0)
                    return Task.FromResult(Result.Fail(CourseValidationMessages.PricePositive));

                if (price > 99_999.99m)
                    return Task.FromResult(Result.Fail(CourseValidationMessages.PriceMax));

                return Task.FromResult(Result.Success());
            });
}

/// <summary>
/// Simple business rule implementation.
/// </summary>
file sealed class BusinessRule(string code, string message, Func<CancellationToken, Task<Result>> checkFunc)
    : IBusinessRule
{
    public string Code => code;
    public string Message => message;
    public Task<Result> CheckAsync(CancellationToken ct) => checkFunc(ct);
}
