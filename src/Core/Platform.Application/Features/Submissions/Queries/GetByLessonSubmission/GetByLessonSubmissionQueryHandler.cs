using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Submissions.Constants;
using Platform.Application.Features.Submissions.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Submissions.Queries.GetByLessonSubmission;

/// <summary>
/// Handler for GetByLessonSubmissionQuery.
/// </summary>
public sealed class GetByLessonSubmissionQueryHandler(
    IReadRepository<Submission> repo) : IRequestHandler<GetByLessonSubmissionQuery, Result<IList<GetAllSubmissionsQueryDto>>>
{
    public async Task<Result<IList<GetAllSubmissionsQueryDto>>> Handle(
        GetByLessonSubmissionQuery request,
        CancellationToken ct)
    {

        var submissions = await repo.GetListAsync(
            s => s.LessonId == request.LessonId,
            ct,
            includes: [s => s.Student]);

        var dtos = submissions.Select(s => new GetAllSubmissionsQueryDto
        {
            Id = s.Id,
            RepoUrl = s.RepoUrl,
            Status = s.Status.ToString(),
            StudentId = s.StudentId,
            StudentName = s.Student != null ? $"{s.Student.FirstName} {s.Student.LastName}" : string.Empty,
            LessonId = s.LessonId,
            LessonTitle = string.Empty,
            CreatedAt = s.CreatedAt
        }).ToList();

        IList<GetAllSubmissionsQueryDto> result = dtos;

        return Result.Success(result);
    }
}
