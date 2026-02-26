using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Courses.Constants;
using Platform.Application.Features.Courses.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Courses.Commands.DeleteCourse;

/// <summary>
/// Handler for DeleteCourseCommand.
/// </summary>
public sealed class DeleteCourseCommandHandler(
    IReadRepository<Course> readRepo,
    IWriteRepository<Course> writeRepo,
    ICourseBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<DeleteCourseCommandHandler> logger) : IRequestHandler<DeleteCourseCommand, Result>
{
    public async Task<Result> Handle(DeleteCourseCommand request, CancellationToken ct)
    {
        // Run business rules
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.CourseMustExist(request.CourseId),
            rules.CourseMustNotBePublished(request.CourseId));

        if (ruleResult.IsFailure)
            return ruleResult;

        // Get course with tracking
        var course = await readRepo.GetByIdAsync(request.CourseId, ct, tracking: true);
        if (course is null)
            return Result.Fail("COURSE_NOT_FOUND", CourseBusinessMessages.NotFoundById);

        // Soft delete using domain method
        course.SoftDelete();

        await writeRepo.UpdateAsync(course, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Course deleted: {CourseId}", course.Id);

        return Result.Success();
    }
}
