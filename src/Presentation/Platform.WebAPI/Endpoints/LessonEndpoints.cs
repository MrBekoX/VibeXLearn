using Asp.Versioning;
using MediatR;
using Platform.Application.Features.Lessons.Commands.CreateLesson;
using Platform.Application.Features.Lessons.Commands.DeleteLesson;
using Platform.Application.Features.Lessons.Commands.MarkLessonAsFree;
using Platform.Application.Features.Lessons.Commands.ReorderLessons;
using Platform.Application.Features.Lessons.Commands.UpdateLesson;
using Platform.Application.Features.Lessons.Queries.GetByCourseLesson;
using Platform.Application.Features.Lessons.Queries.GetByIdLesson;
using Platform.Domain.Enums;
using Platform.WebAPI.Helpers;

namespace Platform.WebAPI.Endpoints;

/// <summary>
/// Lesson endpoints.
/// </summary>
public static class LessonEndpoints
{
    public static IEndpointRouteBuilder RegisterLessonEndpoints(this IEndpointRouteBuilder app)
    {
        var versionedGroup = app.NewVersionedApi("Lessons");

        // Lessons group
        var group = versionedGroup.MapGroup("/api/v{version:apiVersion}/lessons")
            .HasApiVersion(new ApiVersion(1.0))
            .WithTags("Lessons");

        // Course-scoped lesson endpoints
        var courseGroup = versionedGroup.MapGroup("/api/v{version:apiVersion}/courses/{courseId:guid}/lessons")
            .HasApiVersion(new ApiVersion(1.0))
            .WithTags("Lessons");

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetByIdLessonQuery(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error.Message });
        })
        .WithName("GetLessonById");

        courseGroup.MapGet("/", async (Guid courseId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetByCourseLessonQuery(courseId), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("GetLessonsByCourse");

        group.MapPost("/", async (CreateLessonRequest dto, IMediator mediator, CancellationToken ct) =>
        {
            var typeError = EnumHelper.TryParseEnum<LessonType>(dto.Type, "Type", out var lessonType);
            if (typeError is not null)
                return Results.BadRequest(typeError);

            var result = await mediator.Send(new CreateLessonCommand(
                dto.CourseId, dto.Title, dto.Order,
                lessonType,
                dto.Description, dto.VideoUrl, dto.IsFree), ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/lessons/{result.Value}", new { LessonId = result.Value })
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("CreateLesson")
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"));

        group.MapPut("/{id:guid}", async (Guid id, UpdateLessonRequest dto, IMediator mediator, CancellationToken ct) =>
        {
            LessonType? type = null;
            if (dto.Type is not null)
            {
                var typeError = EnumHelper.TryParseEnum<LessonType>(dto.Type, "Type", out var parsedType);
                if (typeError is not null)
                    return Results.BadRequest(typeError);
                type = parsedType;
            }

            var result = await mediator.Send(new UpdateLessonCommand(
                id, dto.Title, dto.Description, dto.VideoUrl,
                dto.Order, type, dto.IsFree), ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("UpdateLesson")
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"));

        group.MapPut("/{id:guid}/mark-free", async (Guid id, MarkLessonAsFreeRequest dto, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new MarkLessonAsFreeCommand(id, dto.IsFree), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("MarkLessonAsFree")
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"));

        courseGroup.MapPut("/reorder", async (Guid courseId, ReorderLessonsRequest dto, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ReorderLessonsCommand(courseId, dto.Lessons), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("ReorderLessons")
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"));

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteLessonCommand(id), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("DeleteLesson")
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"));

        return app;
    }
}

file record CreateLessonRequest(Guid CourseId, string Title, int Order, string Type, string? Description = null, string? VideoUrl = null, bool IsFree = false);
file record UpdateLessonRequest(string? Title = null, string? Description = null, string? VideoUrl = null, int? Order = null, string? Type = null, bool? IsFree = null);
file record MarkLessonAsFreeRequest(bool IsFree = true);
file record ReorderLessonsRequest(IList<Platform.Application.Features.Lessons.DTOs.LessonOrderDto> Lessons);
