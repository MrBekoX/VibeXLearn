using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Courses.Constants;
using Platform.Application.Features.Courses.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Courses.Commands.UpdateCoursePrice;

/// <summary>
/// Handler for UpdateCoursePriceCommand.
/// </summary>
public sealed class UpdateCoursePriceCommandHandler(
    IReadRepository<Course> readRepo,
    IWriteRepository<Course> writeRepo,
    ICourseBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<UpdateCoursePriceCommandHandler> logger) : IRequestHandler<UpdateCoursePriceCommand, Result>
{
    public async Task<Result> Handle(UpdateCoursePriceCommand request, CancellationToken ct)
    {
        // Run business rules
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.CourseMustExist(request.CourseId),
            rules.PriceMustBeValid(request.NewPrice));

        if (ruleResult.IsFailure)
            return ruleResult;

        // Get course with tracking
        var course = await readRepo.GetByIdAsync(request.CourseId, ct, tracking: true);
        if (course is null)
            return Result.Fail("COURSE_NOT_FOUND", CourseBusinessMessages.NotFoundById);

        // Update price using domain method
        course.UpdatePrice(request.NewPrice);

        await writeRepo.UpdateAsync(course, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Course price updated: {CourseId} -> {NewPrice}", course.Id, request.NewPrice);

        return Result.Success();
    }
}
