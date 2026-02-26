using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Common.Rules;
using Platform.Application.Features.Courses.Constants;
using Platform.Application.Features.Courses.Rules;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Courses.Commands.CreateCourse;

/// <summary>
/// Handler for CreateCourseCommand.
/// </summary>
public sealed class CreateCourseCommandHandler(
    IWriteRepository<Course> writeRepo,
    ICourseBusinessRules rules,
    IBusinessRuleEngine ruleEngine,
    IUnitOfWork uow,
    ILogger<CreateCourseCommandHandler> logger) : IRequestHandler<CreateCourseCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCourseCommand request, CancellationToken ct)
    {
        // Run business rules
        var ruleResult = await ruleEngine.RunAsync(ct,
            rules.CategoryMustExist(request.CategoryId),
            rules.InstructorMustExist(request.InstructorId),
            rules.SlugMustBeUnique(request.Slug));

        if (ruleResult.IsFailure)
            return Result.Fail<Guid>(ruleResult.Error);

        // Create course using domain factory method
        var course = Course.Create(
            title: request.Title,
            slug: request.Slug,
            price: request.Price,
            level: request.Level,
            instructorId: request.InstructorId,
            categoryId: request.CategoryId,
            description: request.Description);

        // Set thumbnail if provided
        if (!string.IsNullOrWhiteSpace(request.ThumbnailUrl))
            course.UpdateThumbnail(request.ThumbnailUrl);

        await writeRepo.AddAsync(course, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Course created: {CourseId} - {Title}", course.Id, course.Title);

        return Result.Success(course.Id);
    }
}
