using Asp.Versioning;
using MediatR;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Features.Coupons.Commands.ActivateCoupon;
using Platform.Application.Features.Coupons.Commands.CreateCoupon;
using Platform.Application.Features.Coupons.Commands.DeactivateCoupon;
using Platform.Application.Features.Coupons.Commands.DeleteCoupon;
using Platform.Application.Features.Coupons.Commands.UpdateCoupon;
using Platform.Application.Features.Coupons.Queries.GetAllCoupons;
using Platform.Application.Features.Coupons.Queries.GetByCodeCoupon;
using Platform.Application.Features.Coupons.Queries.GetByIdCoupon;
using Platform.Application.Features.Coupons.Queries.ValidateCoupon;

namespace Platform.WebAPI.Endpoints;

/// <summary>
/// Coupon endpoints.
/// </summary>
public static class CouponEndpoints
{
    public static IEndpointRouteBuilder RegisterCouponEndpoints(this IEndpointRouteBuilder app)
    {
        var versionedGroup = app.NewVersionedApi("Coupons");
        var group = versionedGroup.MapGroup("/api/v{version:apiVersion}/coupons")
            .HasApiVersion(new ApiVersion(1.0))
            .WithTags("Coupons");

        group.MapGet("/", async (
            int page, int pageSize, string? sort,
            IMediator mediator, HttpContext http, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetAllCouponsQuery(new PageRequest
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
        .WithName("GetAllCoupons")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetByIdCouponQuery(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error.Message });
        })
        .WithName("GetCouponById")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapGet("/by-code/{code}", async (string code, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetByCodeCouponQuery(code), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error.Message });
        })
        .WithName("GetCouponByCode")
        .RequireAuthorization();

        group.MapGet("/validate/{code}", async (string code, decimal orderAmount, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ValidateCouponQuery(code, orderAmount), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("ValidateCoupon")
        .RequireAuthorization();

        group.MapPost("/", async (CreateCouponRequest dto, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateCouponCommand(
                dto.Code, dto.DiscountAmount, dto.IsPercentage, dto.UsageLimit, dto.ExpiresAt), ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/coupons/{result.Value}", new { CouponId = result.Value })
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("CreateCoupon")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapPut("/{id:guid}", async (Guid id, UpdateCouponRequest dto, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UpdateCouponCommand(
                id, dto.Code, dto.DiscountAmount, dto.IsPercentage, dto.UsageLimit, dto.ExpiresAt), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("UpdateCoupon")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapPut("/{id:guid}/activate", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ActivateCouponCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("ActivateCoupon")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapPut("/{id:guid}/deactivate", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeactivateCouponCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("DeactivateCoupon")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteCouponCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("DeleteCoupon")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return app;
    }
}

file record CreateCouponRequest(string Code, decimal DiscountAmount, bool IsPercentage, int UsageLimit, DateTime ExpiresAt);
file record UpdateCouponRequest(string? Code = null, decimal? DiscountAmount = null, bool? IsPercentage = null, int? UsageLimit = null, DateTime? ExpiresAt = null);
