using System.Security.Claims;
using Asp.Versioning;
using MediatR;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Features.Orders.Commands.ApplyCoupon;
using Platform.Application.Features.Orders.Commands.CreateOrder;
using Platform.Application.Features.Orders.Queries.GetAllOrders;
using Platform.Application.Features.Orders.Queries.GetByIdOrder;
using Platform.Application.Features.Orders.Queries.GetByUserOrder;

namespace Platform.WebAPI.Endpoints;

/// <summary>
/// Order endpoints.
/// </summary>
public static class OrderEndpoints
{
    public static IEndpointRouteBuilder RegisterOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var versionedGroup = app.NewVersionedApi("Orders");
        var group = versionedGroup.MapGroup("/api/v{version:apiVersion}/orders")
            .HasApiVersion(new ApiVersion(1.0))
            .WithTags("Orders")
            .RequireAuthorization();

        group.MapGet("/", async (
            int page, int pageSize, string? sort,
            IMediator mediator, HttpContext http, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetAllOrdersQuery(new PageRequest
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
        .WithName("GetAllOrders")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetByIdOrderQuery(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error.Message });
        })
        .WithName("GetOrderById");

        group.MapGet("/by-user/{userId:guid}", async (
            Guid userId, int page, int pageSize, string? sort,
            IMediator mediator, HttpContext http, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetByUserOrderQuery(userId, new PageRequest
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
        .WithName("GetOrdersByUser");

        group.MapPost("/", async (
            CreateOrderRequest dto,
            IMediator mediator,
            Platform.Application.Common.Interfaces.ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateOrderCommand(currentUser.GetUserId(), dto.CourseId, dto.CouponCode), ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/orders/{result.Value}", new { OrderId = result.Value })
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("CreateOrder");

        group.MapPut("/{id:guid}/apply-coupon", async (Guid id, ApplyCouponRequest dto, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ApplyCouponCommand(id, dto.CouponCode), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("ApplyCoupon");

        return app;
    }
}

file record CreateOrderRequest(Guid CourseId, string? CouponCode = null);
file record ApplyCouponRequest(string CouponCode);
