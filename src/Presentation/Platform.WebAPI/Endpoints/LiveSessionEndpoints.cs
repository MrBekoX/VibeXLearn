using Asp.Versioning;
using MediatR;
using Platform.Application.Features.LiveSessions.Commands.CancelLiveSession;
using Platform.Application.Features.LiveSessions.Commands.EndLiveSession;
using Platform.Application.Features.LiveSessions.Commands.ScheduleLiveSession;
using Platform.Application.Features.LiveSessions.Commands.StartLiveSession;
using Platform.Application.Features.LiveSessions.Commands.UpdateLiveSession;
using Platform.Application.Features.LiveSessions.Queries.GetByIdLiveSession;

namespace Platform.WebAPI.Endpoints;

/// <summary>
/// Live session endpoints.
/// </summary>
public static class LiveSessionEndpoints
{
    public static IEndpointRouteBuilder RegisterLiveSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var versionedGroup = app.NewVersionedApi("LiveSessions");
        var group = versionedGroup.MapGroup("/api/v{version:apiVersion}/live-sessions")
            .HasApiVersion(new ApiVersion(1.0))
            .WithTags("LiveSessions");

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetByIdLiveSessionQuery(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error.Message });
        })
        .WithName("GetLiveSessionById");

        group.MapPost("/", async (ScheduleLiveSessionRequest dto, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ScheduleLiveSessionCommand(
                dto.LessonId, dto.Topic, dto.StartTime, dto.DurationMin), ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/live-sessions/{result.Value}", new { LiveSessionId = result.Value })
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("ScheduleLiveSession")
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"));

        group.MapPut("/{id:guid}", async (Guid id, UpdateLiveSessionRequest dto, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateLiveSessionCommand(
                id, dto.Topic, dto.StartTime, dto.DurationMin), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("UpdateLiveSession")
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"));

        group.MapPut("/{id:guid}/start", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new StartLiveSessionCommand(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("StartLiveSession")
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"));

        group.MapPut("/{id:guid}/end", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new EndLiveSessionCommand(id), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("EndLiveSession")
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"));

        group.MapPut("/{id:guid}/cancel", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CancelLiveSessionCommand(id), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("CancelLiveSession")
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Instructor"));

        return app;
    }
}

file record ScheduleLiveSessionRequest(Guid LessonId, string Topic, DateTime StartTime, int DurationMin);
file record UpdateLiveSessionRequest(string? Topic = null, DateTime? StartTime = null, int? DurationMin = null);
