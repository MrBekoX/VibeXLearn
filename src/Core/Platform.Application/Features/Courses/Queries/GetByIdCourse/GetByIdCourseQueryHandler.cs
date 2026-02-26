using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Courses.Constants;
using Platform.Application.Features.Courses.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Courses.Queries.GetByIdCourse;

/// <summary>
/// Handler for GetByIdCourseQuery.
/// </summary>
public sealed class GetByIdCourseQueryHandler(
    IReadRepository<Course> readRepo) : IRequestHandler<GetByIdCourseQuery, Result<GetByIdCourseQueryDto>>
{
    public async Task<Result<GetByIdCourseQueryDto>> Handle(
        GetByIdCourseQuery request, CancellationToken ct)
    {

        // Get course with includes
        var course = await readRepo.GetAsync(
            predicate: c => c.Id == request.CourseId,
            ct: ct,
            includes: [c => c.Category, c => c.Instructor, c => c.Lessons]);

        if (course is null)
            return Result.Fail<GetByIdCourseQueryDto>("COURSE_NOT_FOUND", CourseBusinessMessages.NotFoundById);

        // Map to DTO
        var dto = new GetByIdCourseQueryDto
        {
            Id = course.Id,
            Title = course.Title,
            Slug = course.Slug,
            Description = course.Description,
            ThumbnailUrl = course.ThumbnailUrl,
            Price = course.Price,
            Level = course.Level.ToString(),
            Status = course.Status.ToString(),
            EnrollmentCount = course.EnrollmentCount,
            CategoryId = course.CategoryId,
            CategoryName = course.Category?.Name,
            InstructorId = course.InstructorId,
            InstructorName = course.Instructor?.FullName,
            CreatedAt = course.CreatedAt,
            UpdatedAt = course.UpdatedAt,
            Lessons = course.Lessons?
                .OrderBy(l => l.Order)
                .Select(l => new LessonSummaryDto
                {
                    Id = l.Id,
                    Title = l.Title,
                    Order = l.Order,
                    Type = l.Type.ToString(),
                    IsFree = l.IsFree,
                    DurationMinutes = 0 // TODO: Get from video metadata
                }).ToList() ?? []
        };

        return Result.Success(dto);
    }
}
