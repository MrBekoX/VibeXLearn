using Asp.Versioning;
using MediatR;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Features.Courses.Commands.ArchiveCourse;
using Platform.Application.Features.Courses.Commands.CreateCourse;
using Platform.Application.Features.Courses.Commands.DeleteCourse;
using Platform.Application.Features.Courses.Commands.PublishCourse;
using Platform.Application.Features.Courses.Commands.UpdateCourse;
using Platform.Application.Features.Courses.Commands.UpdateCoursePrice;
using Platform.Application.Features.Courses.DTOs;
using Platform.Application.Features.Courses.Queries.GetAllCourses;
using Platform.Application.Features.Courses.Queries.GetByIdCourse;
using Platform.Application.Features.Courses.Queries.GetByInstructorCourse;
using Platform.Application.Features.Courses.Queries.GetBySlugCourse;
using Platform.Domain.Enums;
using Platform.WebAPI.Helpers;

namespace Platform.WebAPI.Endpoints;

/// <summary>
/// Course endpoints.
/// </summary>
public static class CourseEndpoints
{
    public static IEndpointRouteBuilder RegisterCourseEndpoints(this IEndpointRouteBuilder app)
    {
        var versionedGroup = app.NewVersionedApi("Courses");
        var group = versionedGroup.MapGroup("/api/v{version:apiVersion}/courses")
            .HasApiVersion(new ApiVersion(1.0))
            .WithTags("Courses");

        group.MapGet("/", async (
            int page, int pageSize, string? sort, string? search,
            IMediator mediator, HttpContext http, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetAllCoursesQuery(new PageRequest
            {
                Page = page > 0 ? page : 1,
                PageSize = pageSize > 0 ? pageSize : 20,
                Sort = sort,
                Search = search
            }), ct);

            if (result.IsFailure)
                return Results.BadRequest(new { error = result.Error.Message });

            var paged = result.Value;
            http.Response.Headers["X-Pagination"] = System.Text.Json.JsonSerializer.Serialize(paged.ToMetadata());
            return Results.Ok(paged.Items);
        })
        .WithName("GetAllCourses")
        .Produces<IList<GetAllCoursesQueryDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetByIdCourseQuery(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error.Message });
        })
        .WithName("GetCourseById")
        .Produces<GetByIdCourseQueryDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/by-slug/{slug}", async (string slug, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetBySlugCourseQuery(slug), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error.Message });
        })
        .WithName("GetCourseBySlug")
        .Produces<GetBySlugCourseQueryDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/by-instructor/{id:guid}", async (
            Guid id, int page, int pageSize, string? sort,
            IMediator mediator, HttpContext http, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetByInstructorCourseQuery(id, new PageRequest
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
        .WithName("GetCoursesByInstructor")
        .Produces<IList<GetByInstructorCourseQueryDto>>(StatusCodes.Status200OK);

        group.MapPost("/", async (CreateCourseCommandDto dto, IMediator mediator, CancellationToken ct) =>
        {
            var levelError = EnumHelper.TryParseEnum<CourseLevel>(dto.Level, "Level", out var level);
            if (levelError is not null)
                return Results.BadRequest(levelError);

            var result = await mediator.Send(new CreateCourseCommand(
                dto.Title, dto.Slug, dto.Price,
                level,
                dto.InstructorId, dto.CategoryId,
                dto.Description, dto.ThumbnailUrl), ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/courses/{result.Value}", new { CourseId = result.Value })
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("CreateCourse")
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"))
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}", async (Guid id, UpdateCourseCommandDto dto, IMediator mediator, CancellationToken ct) =>
        {
            CourseLevel? level = null;
            if (dto.Level is not null)
            {
                var levelError = EnumHelper.TryParseEnum<CourseLevel>(dto.Level, "Level", out var parsedLevel);
                if (levelError is not null)
                    return Results.BadRequest(levelError);
                level = parsedLevel;
            }

            var result = await mediator.Send(new UpdateCourseCommand(
                id, dto.Title, dto.Description, dto.ThumbnailUrl,
                dto.Price, level, dto.CategoryId), ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("UpdateCourse")
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"))
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}/price", async (Guid id, UpdateCoursePriceRequest req, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateCoursePriceCommand(id, req.NewPrice), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("UpdateCoursePrice")
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"))
        .Produces(StatusCodes.Status204NoContent);

        group.MapPut("/{id:guid}/publish", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new PublishCourseCommand(id), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("PublishCourse")
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"))
        .Produces(StatusCodes.Status204NoContent);

        group.MapPut("/{id:guid}/archive", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ArchiveCourseCommand(id), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("ArchiveCourse")
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .Produces(StatusCodes.Status204NoContent);

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteCourseCommand(id), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("DeleteCourse")
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}

file record UpdateCoursePriceRequest(decimal NewPrice);
