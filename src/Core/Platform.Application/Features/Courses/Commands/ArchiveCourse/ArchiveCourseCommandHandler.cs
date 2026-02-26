using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Courses.Constants;
using Platform.Application.Features.Courses.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Courses.Commands.ArchiveCourse;

/// <summary>
/// Handler for ArchiveCourseCommand.
/// </summary>
public sealed class ArchiveCourseCommandHandler(
    IReadRepository<Course> readRepo,
    IWriteRepository<Course> writeRepo,
    ICourseBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<ArchiveCourseCommandHandler> logger) : IRequestHandler<ArchiveCourseCommand, Result>
{
    public async Task<Result> Handle(ArchiveCourseCommand request, CancellationToken ct)
    {
        // Run business rules
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.CourseMustExist(request.CourseId),
            rules.CourseMustBePublished(request.CourseId));

        if (ruleResult.IsFailure)
            return ruleResult;

        // Get course with tracking
        var course = await readRepo.GetByIdAsync(request.CourseId, ct, tracking: true);
        if (course is null)
            return Result.Fail("COURSE_NOT_FOUND", CourseBusinessMessages.NotFoundById);

        // Archive using domain method
        course.Archive();

        await writeRepo.UpdateAsync(course, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Course archived: {CourseId}", course.Id);

        return Result.Success();
    }
}
