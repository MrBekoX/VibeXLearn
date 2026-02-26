using MediatR;
using Platform.Application.Common.Interfaces;
using Platform.Application.Common.Results;
using Platform.Application.Features.Submissions.Constants;
using Platform.Application.Features.Submissions.DTOs;
using Platform.Domain.Entities;

namespace Platform.Application.Features.Submissions.Queries.GetByIdSubmission;

/// <summary>
/// Handler for GetByIdSubmissionQuery.
/// </summary>
public sealed class GetByIdSubmissionQueryHandler(
    IReadRepository<Submission> repo) : IRequestHandler<GetByIdSubmissionQuery, Result<GetByIdSubmissionQueryDto>>
{
    public async Task<Result<GetByIdSubmissionQueryDto>> Handle(
        GetByIdSubmissionQuery request,
        CancellationToken ct)
    {

        var submission = await repo.GetAsync(
            s => s.Id == request.SubmissionId, ct,
            includes: [s => s.Student, s => s.Lesson, s => s.Lesson.Course]);

        if (submission is null)
            return Result.Fail<GetByIdSubmissionQueryDto>("SUBMISSION_NOT_FOUND", SubmissionBusinessMessages.NotFoundById);

        var dto = new GetByIdSubmissionQueryDto
        {
            Id = submission.Id,
            RepoUrl = submission.RepoUrl,
            CommitSha = submission.CommitSha,
            Branch = submission.Branch,
            PrUrl = submission.PrUrl,
            Status = submission.Status.ToString(),
            ReviewNote = submission.ReviewNote,
            StudentId = submission.StudentId,
            StudentName = submission.Student != null ? $"{submission.Student.FirstName} {submission.Student.LastName}" : string.Empty,
            LessonId = submission.LessonId,
            LessonTitle = submission.Lesson?.Title ?? string.Empty,
            CourseId = submission.Lesson?.CourseId ?? Guid.Empty,
            CourseTitle = submission.Lesson?.Course?.Title ?? string.Empty,
            CreatedAt = submission.CreatedAt,
            UpdatedAt = submission.UpdatedAt
        };
        return Result.Success(dto);
    }
}
