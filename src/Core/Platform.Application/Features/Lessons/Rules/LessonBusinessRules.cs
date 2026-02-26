using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Lessons.Constants;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Lessons.Rules;

public sealed class LessonBusinessRules(
    IReadRepository<Lesson> lessonRepo,
    IReadRepository<Course> courseRepo) : ILessonBusinessRules
{
    public IBusinessRule LessonMustExist(Guid lessonId)
        => new BusinessRule(
            "LESSON_NOT_FOUND",
            LessonBusinessMessages.NotFoundById,
            async ct =>
            {
                var exists = await lessonRepo.AnyAsync(l => l.Id == lessonId, ct);
                return exists ? Result.Success() : Result.Fail(LessonBusinessMessages.NotFoundById);
            });

    public IBusinessRule CourseMustExist(Guid courseId)
        => new BusinessRule(
            "COURSE_NOT_FOUND",
            LessonBusinessMessages.CourseNotFound,
            async ct =>
            {
                var exists = await courseRepo.AnyAsync(c => c.Id == courseId, ct);
                return exists ? Result.Success() : Result.Fail(LessonBusinessMessages.CourseNotFound);
            });

    public IBusinessRule LessonOrderMustBeUnique(Guid courseId, int order, Guid? excludeLessonId = null)
        => new BusinessRule(
            "LESSON_ORDER_DUPLICATE",
            LessonBusinessMessages.DuplicateOrder,
            async ct =>
            {
                var exists = excludeLessonId.HasValue
                    ? await lessonRepo.AnyAsync(
                        l => l.CourseId == courseId && l.Order == order && l.Id != excludeLessonId.Value, ct)
                    : await lessonRepo.AnyAsync(
                        l => l.CourseId == courseId && l.Order == order, ct);
                return !exists ? Result.Success() : Result.Fail(LessonBusinessMessages.DuplicateOrder);
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
