using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Courses.Constants;
using Platform.Application.Features.Courses.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Courses.Commands.UpdateCourse;

/// <summary>
/// Handler for UpdateCourseCommand.
/// </summary>
public sealed class UpdateCourseCommandHandler(
    IReadRepository<Course> readRepo,
    IWriteRepository<Course> writeRepo,
    ICourseBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<UpdateCourseCommandHandler> logger) : IRequestHandler<UpdateCourseCommand, Result>
{
    public async Task<Result> Handle(UpdateCourseCommand request, CancellationToken ct)
    {
        // Get course with tracking for update
        var course = await readRepo.GetByIdAsync(request.CourseId, ct, tracking: true);
        if (course is null)
            return Result.Fail("COURSE_NOT_FOUND", CourseBusinessMessages.NotFoundById);

        // Validate category if changing
        if (request.CategoryId.HasValue)
        {
            var ruleResult = await ruleEngine.RunAsync(ct,
                rules.CategoryMustExist(request.CategoryId.Value));
            if (ruleResult.IsFailure)
                return ruleResult;
        }

        // Apply updates using domain methods
        if (!string.IsNullOrWhiteSpace(request.Title))
            course.UpdateTitle(request.Title);

        if (request.Description is not null)
            course.UpdateDescription(request.Description);

        if (request.ThumbnailUrl is not null)
            course.UpdateThumbnail(request.ThumbnailUrl);

        if (request.Price.HasValue)
            course.UpdatePrice(request.Price.Value);

        if (request.Level.HasValue)
            course.UpdateLevel(request.Level.Value);

        if (request.CategoryId.HasValue)
            course.UpdateCategory(request.CategoryId.Value);

        await writeRepo.UpdateAsync(course, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Course updated: {CourseId}", course.Id);

        return Result.Success();
    }
}
