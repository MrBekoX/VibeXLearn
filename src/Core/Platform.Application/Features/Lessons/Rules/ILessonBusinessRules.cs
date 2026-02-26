using Platform.Application.Common.Rules;

namespace Platform.Application.Features.Lessons.Rules;

public interface ILessonBusinessRules
{
    IBusinessRule LessonMustExist(Guid lessonId);
    IBusinessRule CourseMustExist(Guid courseId);
    IBusinessRule LessonOrderMustBeUnique(Guid courseId, int order, Guid? excludeLessonId = null);
}
