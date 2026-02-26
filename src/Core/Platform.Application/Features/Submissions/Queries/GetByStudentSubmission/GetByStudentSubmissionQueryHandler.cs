using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Submissions.Constants;
using Platform.Application.Features.Submissions.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Submissions.Queries.GetByStudentSubmission;

/// <summary>
/// Handler for GetByStudentSubmissionQuery.
/// </summary>
public sealed class GetByStudentSubmissionQueryHandler(
    IReadRepository<Submission> repo) : IRequestHandler<GetByStudentSubmissionQuery, Result<IList<GetByStudentSubmissionQueryDto>>>
{
    public async Task<Result<IList<GetByStudentSubmissionQueryDto>>> Handle(
        GetByStudentSubmissionQuery request,
        CancellationToken ct)
    {

        var submissions = await repo.GetListAsync(
            s => s.StudentId == request.StudentId,
            ct,
            includes: [s => s.Lesson, s => s.Lesson.Course]);

        var dtos = submissions.Select(s => new GetByStudentSubmissionQueryDto
        {
            Id = s.Id,
            LessonId = s.LessonId,
            LessonTitle = s.Lesson?.Title ?? string.Empty,
            CourseId = s.Lesson?.CourseId ?? Guid.Empty,
            CourseTitle = s.Lesson?.Course?.Title ?? string.Empty,
            RepoUrl = s.RepoUrl,
            Status = s.Status.ToString(),
            CreatedAt = s.CreatedAt
        }).ToList();

        IList<GetByStudentSubmissionQueryDto> result = dtos;

        return Result.Success(result);
    }
}
