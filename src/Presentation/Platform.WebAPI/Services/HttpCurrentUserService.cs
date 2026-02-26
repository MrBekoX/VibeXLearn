using System.Security.Claims;
using Platform.Application.Common.Interfaces;

namespace Platform.WebAPI.Services;

/// <summary>
/// Resolves current user information from HTTP context JWT claims.
/// Registered as scoped — one instance per request.
/// </summary>
public class HttpCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CurrentUserDto GetCurrentUser()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
            throw new UnauthorizedAccessException("User is not authenticated");

        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user identifier");

        return new CurrentUserDto(
            UserId: userId,
            Email: user.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            FirstName: user.FindFirstValue("firstName") ?? string.Empty,
            LastName: user.FindFirstValue("lastName") ?? string.Empty
        );
    }

    public Guid GetUserId()
    {
        return GetCurrentUser().UserId;
    }

    public bool IsAuthenticated()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
    }

    public bool IsInRole(string role)
    {
        return _httpContextAccessor.HttpContext?.User?.IsInRole(role) == true;
    }
}
