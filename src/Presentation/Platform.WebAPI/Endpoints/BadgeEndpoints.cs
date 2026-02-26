using Asp.Versioning;
using MediatR;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Features.Badges.Commands.CreateBadge;
using Platform.Application.Features.Badges.Commands.DeleteBadge;
using Platform.Application.Features.Badges.Commands.UpdateBadge;
using Platform.Application.Features.Badges.Queries.GetAllBadges;
using Platform.Application.Features.Badges.Queries.GetByIdBadge;

namespace Platform.WebAPI.Endpoints;

/// <summary>
/// Badge endpoints.
/// </summary>
public static class BadgeEndpoints
{
    public static IEndpointRouteBuilder RegisterBadgeEndpoints(this IEndpointRouteBuilder app)
    {
        var versionedGroup = app.NewVersionedApi("Badges");
        var group = versionedGroup.MapGroup("/api/v{version:apiVersion}/badges")
            .HasApiVersion(new ApiVersion(1.0))
            .WithTags("Badges");

        group.MapGet("/", async (
            int page, int pageSize, string? sort,
            IMediator mediator, HttpContext http, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetAllBadgesQuery(new PageRequest
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
        .WithName("GetAllBadges");

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetByIdBadgeQuery(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error.Message });
        })
        .WithName("GetBadgeById");

        group.MapPost("/", async (CreateBadgeRequest dto, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateBadgeCommand(
                dto.Name, dto.Description, dto.IconUrl, dto.Criteria), ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/badges/{result.Value}", new { BadgeId = result.Value })
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("CreateBadge")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapPut("/{id:guid}", async (Guid id, UpdateBadgeRequest dto, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateBadgeCommand(id, dto.Name, dto.Description, dto.IconUrl), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("UpdateBadge")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteBadgeCommand(id), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("DeleteBadge")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return app;
    }
}

file record CreateBadgeRequest(string Name, string Description, string IconUrl, string Criteria);
file record UpdateBadgeRequest(string? Name = null, string? Description = null, string? IconUrl = null);
