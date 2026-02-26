using System.Security.Claims;
using Asp.Versioning;
using MediatR;
using Platform.Application.Features.Submissions.Commands.CreateSubmission;
using Platform.Application.Features.Submissions.Commands.ReviewSubmission;
using Platform.Application.Features.Submissions.Queries.GetByIdSubmission;
using Platform.Application.Features.Submissions.Queries.GetByLessonSubmission;
using Platform.Application.Features.Submissions.Queries.GetByStudentSubmission;

namespace Platform.WebAPI.Endpoints;

/// <summary>
/// Submission endpoints.
/// </summary>
public static class SubmissionEndpoints
{
    public static IEndpointRouteBuilder RegisterSubmissionEndpoints(this IEndpointRouteBuilder app)
    {
        var versionedGroup = app.NewVersionedApi("Submissions");
        var group = versionedGroup.MapGroup("/api/v{version:apiVersion}/submissions")
            .HasApiVersion(new ApiVersion(1.0))
            .WithTags("Submissions")
            .RequireAuthorization();

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetByIdSubmissionQuery(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error.Message });
        })
        .WithName("GetSubmissionById");

        group.MapGet("/by-student/{studentId:guid}", async (Guid studentId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetByStudentSubmissionQuery(studentId), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("GetSubmissionsByStudent");

        group.MapGet("/by-lesson/{lessonId:guid}", async (Guid lessonId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetByLessonSubmissionQuery(lessonId), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("GetSubmissionsByLesson")
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"));

        group.MapPost("/", async (
            CreateSubmissionRequest dto,
            IMediator mediator,
            Platform.Application.Common.Interfaces.ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateSubmissionCommand(
                currentUser.GetUserId(), dto.LessonId, dto.RepoUrl, dto.CommitSha, dto.Branch), ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/submissions/{result.Value}", new { SubmissionId = result.Value })
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("CreateSubmission");

        group.MapPut("/{id:guid}/review", async (Guid id, ReviewSubmissionRequest dto, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ReviewSubmissionCommand(id, dto.Accept, dto.ReviewNote), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("ReviewSubmission")
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"));

        return app;
    }
}

file record CreateSubmissionRequest(Guid LessonId, string RepoUrl, string? CommitSha = null, string? Branch = null);
file record ReviewSubmissionRequest(bool Accept, string? ReviewNote = null);
