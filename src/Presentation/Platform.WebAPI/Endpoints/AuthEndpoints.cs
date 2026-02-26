using System.Security.Claims;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.RateLimiting;
using Platform.Application.Features.Auth.Commands.Login;
using Platform.Application.Features.Auth.Commands.Logout;
using Platform.Application.Features.Auth.Commands.RefreshToken;
using Platform.Application.Features.Auth.Commands.Register;
using Platform.Application.Features.Auth.DTOs;
using Platform.Application.Features.Auth.Queries.GetProfile;

namespace Platform.WebAPI.Endpoints;

/// <summary>
/// Authentication endpoints.
/// </summary>
public static class AuthEndpoints
{
    private const string RefreshTokenCookieName = "refreshToken";

    public static IEndpointRouteBuilder RegisterAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var versionedGroup = app.NewVersionedApi("Auth");
        var group = versionedGroup.MapGroup("/api/v{version:apiVersion}/auth")
            .HasApiVersion(new ApiVersion(1.0))
            .WithTags("Auth")
            .RequireRateLimiting("AuthPolicy");

        group.MapPost("/register", async (RegisterCommandDto dto, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new RegisterCommand(dto.Email, dto.Password, dto.FirstName, dto.LastName), ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/auth/profile", new { UserId = result.Value })
                : Results.BadRequest(new { error = result.Error.Message });
        })
        .WithName("Register")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/login", async (LoginCommandDto dto, IMediator mediator, HttpContext http, CancellationToken ct) =>
        {
            var result = await mediator.Send(new LoginCommand(dto.Email, dto.Password), ct);

            if (result.IsFailure)
                return Results.Unauthorized();

            var loginResult = result.Value;

            // Set refresh token as HttpOnly cookie — never in response body
            http.Response.Cookies.Append(RefreshTokenCookieName, loginResult.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                Path = "/api/v1/auth"
            });

            return Results.Ok(new LoginResponseDto
            {
                AccessToken = loginResult.AccessToken,
                ExpiresAt = loginResult.ExpiresAt
            });
        })
        .WithName("Login")
        .Produces<LoginResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", async (IMediator mediator, HttpContext http, CancellationToken ct) =>
        {
            // Read refresh token from HttpOnly cookie
            var refreshToken = http.Request.Cookies[RefreshTokenCookieName];
            if (string.IsNullOrWhiteSpace(refreshToken))
                return Results.Unauthorized();

            var result = await mediator.Send(new RefreshTokenCommand(refreshToken), ct);

            if (result.IsFailure)
                return Results.Unauthorized();

            var refreshResult = result.Value;

            // Rotate: set new refresh token cookie
            http.Response.Cookies.Append(RefreshTokenCookieName, refreshResult.NewRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                Path = "/api/v1/auth"
            });

            return Results.Ok(new RefreshTokenResponseDto
            {
                AccessToken = refreshResult.AccessToken,
                ExpiresAt = refreshResult.ExpiresAt
            });
        })
        .WithName("RefreshToken")
        .Produces<RefreshTokenResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", async (
            IMediator mediator,
            Application.Common.Interfaces.ICurrentUserService currentUser,
            HttpContext http,
            CancellationToken ct) =>
        {
            var jti = http.User.FindFirstValue(
                System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti);

            await mediator.Send(new LogoutCommand(currentUser.GetUserId(), jti), ct);

            // Clear the refresh token cookie
            http.Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/api/v1/auth"
            });

            return Results.NoContent();
        })
        .WithName("Logout")
        .RequireAuthorization()
        .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/profile", async (
            IMediator mediator,
            Application.Common.Interfaces.ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetProfileQuery(currentUser.GetUserId()), ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error.Message });
        })
        .WithName("GetProfile")
        .RequireAuthorization()
        .Produces<UserProfileDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        // Admin-only: manual token cleanup
        group.MapPost("/cleanup-tokens", async (
            Application.Common.Interfaces.ITokenCleanupService cleanupService,
            CancellationToken ct) =>
        {
            var count = await cleanupService.CleanupAsync(ct);
            return Results.Ok(new { deletedCount = count });
        })
        .WithName("CleanupTokens")
        .RequireAuthorization(p => p.RequireRole("Admin"))
        .Produces(StatusCodes.Status200OK);

        return app;
    }
}
