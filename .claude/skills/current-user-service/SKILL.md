---
name: current-user-service
description: Implement ICurrentUserService to avoid repetitive claim parsing in endpoints.
---

# Current User Service Implementation

Implement ICurrentUserService to centralize user information access from HTTP context.

## Problem

```csharp
// ❌ BAD: Repetitive claim parsing in every endpoint
group.MapPost("/logout", async (IMediator mediator, HttpContext http, CancellationToken ct) =>
{
    var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();
    
    await mediator.Send(new LogoutCommand(userId), ct);
    // ...
});

group.MapGet("/profile", async (IMediator mediator, HttpContext http, CancellationToken ct) =>
{
    var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!Guid.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();
    
    var result = await mediator.Send(new GetProfileQuery(userId), ct);
    // ...
});
// Repeated in multiple endpoints...
```

## Solution

### Step 1: Implement ICurrentUserService

```csharp
// Platform.WebAPI/Services/HttpCurrentUserService.cs
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
        var user = GetCurrentUser();
        return user.UserId;
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
```

### Step 2: Extended Interface

```csharp
// Platform.Application/Common/Interfaces/ICurrentUserService.cs
public interface ICurrentUserService
{
    CurrentUserDto GetCurrentUser();
    Guid GetUserId();
    bool IsAuthenticated();
    bool IsInRole(string role);
}

public sealed record CurrentUserDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
}
```

### Step 3: Registration

```csharp
// Program.cs
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, HttpCurrentUserService>();
```

### Step 4: Usage in Endpoints

```csharp
// ✅ GOOD: Clean endpoint with ICurrentUserService
group.MapPost("/logout", async (
    IMediator mediator, 
    ICurrentUserService currentUser,
    HttpContext http, 
    CancellationToken ct) =>
{
    await mediator.Send(new LogoutCommand(currentUser.GetUserId()), ct);

    http.Response.Cookies.Delete("refreshToken", new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api/v1/auth"
    });

    return Results.NoContent();
})
.RequireAuthorization();

group.MapGet("/profile", async (
    IMediator mediator, 
    ICurrentUserService currentUser,
    CancellationToken ct) =>
{
    var result = await mediator.Send(new GetProfileQuery(currentUser.GetUserId()), ct);
    
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.NotFound(new { error = result.Error.Message });
})
.RequireAuthorization();
```

### Step 5: Usage in Handlers

```csharp
public class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, Result<Guid>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ICourseRepository _repository;

    public CreateCourseCommandHandler(
        ICurrentUserService currentUser,
        ICourseRepository repository)
    {
        _currentUser = currentUser;
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreateCourseCommand request, CancellationToken ct)
    {
        // Automatically get current user
        var instructorId = _currentUser.GetUserId();

        // Verify user has instructor role
        if (!_currentUser.IsInRole("Instructor") && !_currentUser.IsInRole("Admin"))
            return Result.Fail<Guid>("FORBIDDEN", "Only instructors can create courses");

        var course = Course.Create(
            request.Title,
            request.Slug,
            request.Price,
            request.Level,
            instructorId,  // Use from service
            request.CategoryId);

        await _repository.AddAsync(course, ct);
        return Result.Success(course.Id);
    }
}
```

## Alternative: Custom Middleware

```csharp
// Middleware to populate ICurrentUser per request
public class CurrentUserMiddleware
{
    private readonly RequestDelegate _next;

    public CurrentUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context, 
        ICurrentUserService currentUserService)
    {
        // Pre-populate user info for the request
        if (currentUserService.IsAuthenticated())
        {
            var user = currentUserService.GetCurrentUser();
            context.Items["CurrentUser"] = user;
        }

        await _next(context);
    }
}

// Usage
app.UseMiddleware<CurrentUserMiddleware>();
```

## Benefits

1. **DRY**: No repetitive claim parsing
2. **Testable**: Easy to mock in unit tests
3. **Type-safe**: Returns strongly-typed DTO
4. **Centralized**: Single place for user logic
5. **Clean**: Endpoints focus on business logic
