using Asp.Versioning;
using MediatR;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Features.Categories.Commands.CreateCategory;
using Platform.Application.Features.Categories.Commands.DeleteCategory;
using Platform.Application.Features.Categories.Commands.UpdateCategory;
using Platform.Application.Features.Categories.Queries.GetAllCategories;
using Platform.Application.Features.Categories.Queries.GetByIdCategory;
using Platform.Application.Features.Categories.Queries.GetBySlugCategory;
using Platform.Application.Features.Categories.Queries.GetCategoryTree;

namespace Platform.WebAPI.Endpoints;

/// <summary>
/// Category endpoints.
/// </summary>
public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder RegisterCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var versionedGroup = app.NewVersionedApi("Categories");
        var group = versionedGroup.MapGroup("/api/v{version:apiVersion}/categories")
            .HasApiVersion(new ApiVersion(1.0))
            .WithTags("Categories");

        group.MapGet("/", async (
            int page, int pageSize, string? sort, string? search,
            IMediator mediator, HttpContext http, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetAllCategoriesQuery(new PageRequest
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
        .WithName("GetAllCategories");

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetByIdCategoryQuery(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error.Message });
        })
        .WithName("GetCategoryById");

        group.MapGet("/by-slug/{slug}", async (string slug, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetBySlugCategoryQuery(slug), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error.Message });
        })
        .WithName("GetCategoryBySlug");

        group.MapGet("/tree", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetCategoryTreeQuery(), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("GetCategoryTree");

        group.MapPost("/", async (CreateCategoryRequest dto, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateCategoryCommand(
                dto.Name, dto.Slug, dto.Description, dto.ParentId), ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/categories/{result.Value}", new { CategoryId = result.Value })
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("CreateCategory")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapPut("/{id:guid}", async (Guid id, UpdateCategoryRequest dto, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateCategoryCommand(id, dto.Name, dto.Description), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("UpdateCategory")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteCategoryCommand(id), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("DeleteCategory")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return app;
    }
}

file record CreateCategoryRequest(string Name, string Slug, string? Description = null, Guid? ParentId = null);
file record UpdateCategoryRequest(string? Name = null, string? Description = null);
