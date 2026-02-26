using System.Security.Claims;
using Asp.Versioning;
using MediatR;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Features.Enrollments.Commands.CancelEnrollment;
using Platform.Application.Features.Enrollments.Commands.CompleteEnrollment;
using Platform.Application.Features.Enrollments.Commands.CreateEnrollment;
using Platform.Application.Features.Enrollments.Commands.UpdateEnrollmentProgress;
using Platform.Application.Features.Enrollments.Queries.GetAllEnrollments;
using Platform.Application.Features.Enrollments.Queries.GetByCourseEnrollment;
using Platform.Application.Features.Enrollments.Queries.GetByIdEnrollment;
using Platform.Application.Features.Enrollments.Queries.GetByUserEnrollment;

namespace Platform.WebAPI.Endpoints;

/// <summary>
/// Enrollment endpoints.
/// </summary>
public static class EnrollmentEndpoints
{
    public static IEndpointRouteBuilder RegisterEnrollmentEndpoints(this IEndpointRouteBuilder app)
    {
        var versionedGroup = app.NewVersionedApi("Enrollments");
        var group = versionedGroup.MapGroup("/api/v{version:apiVersion}/enrollments")
            .HasApiVersion(new ApiVersion(1.0))
            .WithTags("Enrollments")
            .RequireAuthorization();

        group.MapGet("/", async (
            int page, int pageSize, string? sort,
            IMediator mediator, HttpContext http, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetAllEnrollmentsQuery(new PageRequest
            {
                Page = page > 0 ? page : 1,
                PageSize = pageSize > 0 ? pageSize : 20,
                Sort = sort
            }), ct);

            if (result.IsFailure)
                return Results.BadRequest(new { error = result.Error.Message });

            var paged = result.Value;
            http.Response.Headers["X-Pagination"] = System.Text.Json.JsonSerializer.Serialize(paged.ToMetadata());
            return Results.Ok(paged.Items);
        })
        .WithName("GetAllEnrollments")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetByIdEnrollmentQuery(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error.Message });
        })
        .WithName("GetEnrollmentById");

        group.MapGet("/by-user/{userId:guid}", async (
            Guid userId, int page, int pageSize, string? sort,
            IMediator mediator, HttpContext http, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetByUserEnrollmentQuery(userId, new PageRequest
            {
                Page = page > 0 ? page : 1,
                PageSize = pageSize > 0 ? pageSize : 20,
                Sort = sort
            }), ct);

            if (result.IsFailure)
                return Results.BadRequest(new { error = result.Error.Message });

            var paged = result.Value;
            http.Response.Headers["X-Pagination"] = System.Text.Json.JsonSerializer.Serialize(paged.ToMetadata());
            return Results.Ok(paged.Items);
        })
        .WithName("GetEnrollmentsByUser");

        group.MapGet("/by-course/{courseId:guid}", async (
            Guid courseId, int page, int pageSize, string? sort,
            IMediator mediator, HttpContext http, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetByCourseEnrollmentQuery(courseId, new PageRequest
            {
                Page = page > 0 ? page : 1,
                PageSize = pageSize > 0 ? pageSize : 20,
                Sort = sort
            }), ct);

            if (result.IsFailure)
                return Results.BadRequest(new { error = result.Error.Message });

            var paged = result.Value;
            http.Response.Headers["X-Pagination"] = System.Text.Json.JsonSerializer.Serialize(paged.ToMetadata());
            return Results.Ok(paged.Items);
        })
        .WithName("GetEnrollmentsByCourse")
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"));

        group.MapPost("/", async (
            CreateEnrollmentRequest dto,
            IMediator mediator,
            Platform.Application.Common.Interfaces.ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateEnrollmentCommand(currentUser.GetUserId(), dto.CourseId), ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/enrollments/{result.Value}", new { EnrollmentId = result.Value })
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("CreateEnrollment");

        group.MapPut("/{id:guid}/progress", async (Guid id, UpdateProgressRequest dto, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateEnrollmentProgressCommand(id, dto.Progress), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("UpdateEnrollmentProgress");

        group.MapPut("/{id:guid}/complete", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CompleteEnrollmentCommand(id), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("CompleteEnrollment");

        group.MapPut("/{id:guid}/cancel", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CancelEnrollmentCommand(id), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("CancelEnrollment");

        return app;
    }
}

file record CreateEnrollmentRequest(Guid CourseId);
file record UpdateProgressRequest(decimal Progress);
