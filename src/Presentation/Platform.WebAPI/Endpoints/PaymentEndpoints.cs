using System.Security.Claims;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.RateLimiting;
using Platform.Application.Common.Models.Pagination;
using Platform.Application.Features.Payments.Commands.InitiateCheckout;
using Platform.Application.Features.Payments.Commands.ProcessCallback;
using Platform.Application.Features.Payments.Queries.GetAllPayments;
using Platform.Application.Features.Payments.Queries.GetByIdPayment;
using Platform.Application.Features.Payments.Queries.GetByOrderPayment;

namespace Platform.WebAPI.Endpoints;

/// <summary>
/// Payment endpoints.
/// </summary>
public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder RegisterPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var versionedGroup = app.NewVersionedApi("Payments");
        var group = versionedGroup.MapGroup("/api/v{version:apiVersion}/payments")
            .HasApiVersion(new ApiVersion(1.0))
            .WithTags("Payments");

        group.MapGet("/", async (
            int page, int pageSize, string? sort,
            IMediator mediator, HttpContext http, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetAllPaymentsQuery(new PageRequest
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
        .WithName("GetAllPayments")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetByIdPaymentQuery(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error.Message });
        })
        .WithName("GetPaymentById")
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapGet("/by-order/{orderId:guid}", async (Guid orderId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetByOrderPaymentQuery(orderId), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error.Message });
        })
        .WithName("GetPaymentByOrder")
        .RequireAuthorization();

        group.MapPost("/checkout", async (
            CheckoutRequest dto,
            IMediator mediator,
            Platform.Application.Common.Interfaces.ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new InitiateCheckoutCommand(currentUser.GetUserId(), dto.CourseId, dto.CouponCode), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("InitiateCheckout")
        .RequireAuthorization()
        .RequireRateLimiting("PaymentPolicy");

        group.MapPost("/callback", async (
            IMediator mediator,
            HttpContext http,
            IConfiguration config,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("PaymentCallbacks");

            // SKILL: fix-payment-security-issues - IP whitelist validation
            var remoteIp = http.Connection.RemoteIpAddress?.ToString();
            var isProduction = config["Iyzico:Environment"]?.Equals("Production",
                StringComparison.OrdinalIgnoreCase) ?? false;

            if (!Platform.Integration.Iyzico.IyzicoIpWhitelist.IsAllowed(remoteIp, isProduction))
            {
                logger.LogWarning(
                    "{Event} | IP:{Ip} | Reason:IP_NOT_WHITELISTED",
                    "PAYMENT_CALLBACK_REJECTED", remoteIp);
                // Return 200 to prevent retry storms, but don't process
                return Results.Ok(new { status = "received" });
            }

            // Read raw body for audit trail
            http.Request.EnableBuffering();
            using var reader = new StreamReader(http.Request.Body, leaveOpen: true);
            var rawBody = await reader.ReadToEndAsync(ct);
            http.Request.Body.Position = 0;

            // Read form values
            var form = await http.Request.ReadFormAsync(ct);
            var token = form["token"].FirstOrDefault() ?? string.Empty;
            var conversationId = form["conversationId"].FirstOrDefault() ?? string.Empty;

            // Validate required parameters
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(conversationId))
            {
                logger.LogWarning(
                    "{Event} | IP:{Ip} | Reason:MISSING_PARAMS",
                    "PAYMENT_CALLBACK_INVALID", remoteIp);
                return Results.Ok(new { status = "received" });
            }

            logger.LogInformation(
                "{Event} | ConvId:{ConvId} | IP:{Ip}",
                "PAYMENT_CALLBACK", conversationId[..Math.Min(16, conversationId.Length)] + "***", remoteIp);

            await mediator.Send(new ProcessCallbackCommand(token, conversationId, rawBody), ct);

            // Always return 200 — prevents Iyzico retry storm
            return Results.Ok(new { status = "received" });
        })
        .WithName("PaymentCallback")
        .AllowAnonymous()
        .ExcludeFromDescription();

        return app;
    }
}

file record CheckoutRequest(Guid CourseId, string? CouponCode = null);
