---
name: test-coverage
description: Add comprehensive test coverage to .NET projects with xUnit, including unit tests, integration tests, and best practices for testing Onion Architecture applications.
---

# Test Coverage Implementation

Implement comprehensive test coverage for .NET applications using xUnit with proper project structure and architectural testing patterns.

## Quick Start

```bash
# Create test projects
mkdir tests
dotnet new xunit -n Platform.Domain.Tests -o tests/Platform.Domain.Tests
dotnet new xunit -n Platform.Application.Tests -o tests/Platform.Application.Tests
dotnet new xunit -n Platform.Integration.Tests -o tests/Platform.Integration.Tests

# Add references
cd tests/Platform.Domain.Tests
dotnet add reference ../../src/Core/Platform.Domain
```

## Project Structure

```
tests/
├── Platform.Domain.Tests/          # Pure domain logic tests
│   ├── Entities/
│   ├── ValueObjects/
│   └── DomainEvents/
├── Platform.Application.Tests/     # Use case tests (with mocks)
│   ├── Features/
│   │   └── Auth/
│   │       └── Commands/
│   └── Behaviours/
└── Platform.Integration.Tests/     # Full integration tests
    ├── Api/
    ├── Database/
    └── Cache/
```

## Domain Testing

```csharp
// Platform.Domain.Tests/Entities/CourseTests.cs
public class CourseTests
{
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var course = Course.Create(
            "Test Course", 
            "test-course", 
            100m, 
            CourseLevel.Beginner,
            Guid.NewGuid(), 
            Guid.NewGuid());

        course.Should().NotBeNull();
        course.Status.Should().Be(CourseStatus.Draft);
        course.Price.Should().Be(100m);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_WithInvalidTitle_Throws(string invalidTitle)
    {
        Action act = () => Course.Create(
            invalidTitle, 
            "test-slug", 
            100m,
            CourseLevel.Beginner,
            Guid.NewGuid(), 
            Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Publish_DraftCourse_Succeeds()
    {
        var course = CreateTestCourse();
        
        course.Publish();
        
        course.Status.Should().Be(CourseStatus.Published);
        course.DomainEvents.Should().ContainSingle(e => e is CoursePublishedEvent);
    }

    [Fact]
    public void Publish_AlreadyPublished_Throws()
    {
        var course = CreateTestCourse();
        course.Publish();

        Action act = () => course.Publish();

        act.Should().Throw<DomainException>()
            .WithMessage("*Only draft courses can be published*");
    }
}
```

## Application Testing (with Moq)

```csharp
// Platform.Application.Tests/Features/Auth/LoginCommandHandlerTests.cs
public class LoginCommandHandlerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<ILogger<LoginCommandHandler>> _loggerMock;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _loggerMock = new Mock<ILogger<LoginCommandHandler>>();
        _handler = new LoginCommandHandler(
            _authServiceMock.Object, 
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var command = new LoginCommand("test@test.com", "password123");
        var expectedResult = Result.Success(("access_token", DateTime.UtcNow.AddMinutes(15), "refresh_token"));
        
        _authServiceMock
            .Setup(x => x.LoginAsync(command.Email, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access_token");
    }

    [Fact]
    public async Task Handle_InvalidCredentials_LogsWarning()
    {
        // Arrange
        var command = new LoginCommand("test@test.com", "wrong");
        var failResult = Result.Fail<(string, DateTime, string)>(
            "AUTH_INVALID_CREDENTIALS", "Invalid credentials");
        
        _authServiceMock
            .Setup(x => x.LoginAsync(command.Email, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("LoginFailed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
```

## Integration Testing

```csharp
// Platform.Integration.Tests/Api/AuthEndpointsTests.cs
public class AuthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public AuthEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real services with test containers
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(opt =>
                    opt.UseNpgsql(_postgresContainer.GetConnectionString()));
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidData_Returns201()
    {
        // Arrange
        var request = new RegisterCommandDto
        {
            Email = "newuser@test.com",
            Password = "SecurePass123",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        content!.UserId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        // Arrange
        var request = new LoginCommandDto
        {
            Email = "nonexistent@test.com",
            Password = "wrongpassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

## Test Containers Setup

```csharp
// Platform.Integration.Tests/Fixtures/IntegrationTestFixture.cs
public class IntegrationTestFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; private set; } = null!;
    public RedisContainer Redis { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Postgres = new PostgreSqlBuilder()
            .WithDatabase("test_db")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        Redis = new RedisContainerBuilder()
            .Build();

        await Postgres.StartAsync();
        await Redis.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Postgres.DisposeAsync();
        await Redis.DisposeAsync();
    }
}
```

## CI/CD Integration

```yaml
# .github/workflows/test.yml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Restore
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore
      
      - name: Test with coverage
        run: |
          dotnet test --no-build \
            --collect:"XPlat Code Coverage" \
            --logger trx \
            --results-directory ./coverage
      
      - name: Upload coverage
        uses: codecov/codecov-action@v3
        with:
          directory: ./coverage
```

## Coverage Thresholds

```xml
<!-- Directory.Build.props -->
<PropertyGroup>
    <CollectCoverage>true</CollectCoverage>
    <CoverletOutputFormat>cobertura</CoverletOutputFormat>
    <Threshold>80</Threshold>
    <ThresholdType>line,method</ThresholdType>
</PropertyGroup>
```

## Best Practices

1. **Follow AAA Pattern**: Arrange, Act, Assert
2. **One Assert Per Test**: Single responsibility for tests
3. **Use Meaningful Names**: `Should_Throw_When_Email_Is_Invalid`
4. **Avoid Logic in Tests**: No loops or conditionals
5. **Test Public Behavior**: Don't test private methods
6. **Isolate Tests**: Each test should be independent
7. **Use Builders/Factories**: For complex test data setup
