using FluentValidation;
using Platform.Infrastructure.Extensions;
using Platform.Infrastructure.Middlewares;
using Platform.Integration.Extensions;
using Platform.Persistence.Extensions;
using Platform.WebAPI.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// SKILL: secrets-from-config - Validate configuration at startup
builder.ValidateConfiguration();

// SKILL: request-size-limits - Configure Kestrel limits
builder.WebHost.ConfigureKestrel(kestrelOptions =>
{
    kestrelOptions.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
    kestrelOptions.Limits.MaxRequestHeadersTotalSize = 32 * 1024; // 32KB
    kestrelOptions.Limits.MaxRequestHeaderCount = 100;
    kestrelOptions.Limits.MaxRequestLineSize = 8 * 1024; // 8KB
    kestrelOptions.Limits.MaxConcurrentConnections = 100;
    kestrelOptions.Limits.MaxConcurrentUpgradedConnections = 100;
    kestrelOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    kestrelOptions.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});

// Configure form options for multipart uploads
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(formOptions =>
{
    formOptions.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
    formOptions.ValueLengthLimit = int.MaxValue;
    formOptions.MultipartHeadersLengthLimit = 32 * 1024; // 32KB
    formOptions.BufferBodyLengthLimit = 10 * 1024 * 1024; // 10MB
});

// ── Serilog (SKILL: fix-logging-security) ───────────────────────────────────
var elasticUrl = builder.Configuration.GetConnectionString("Elasticsearch")
    ?? "http://localhost:9200";

var loggerConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.With<Platform.Infrastructure.Logging.SensitiveDataEnricher>() // Mask sensitive data
    .WriteTo.Console();

// Add Elasticsearch with authentication if configured
var elasticUsername = builder.Configuration["Elastic:Username"];
var elasticPassword = builder.Configuration["Elastic:Password"];

if (!string.IsNullOrEmpty(elasticUsername) && !string.IsNullOrEmpty(elasticPassword))
{
    // Production: Elasticsearch with authentication
    loggerConfig.WriteTo.Elasticsearch(new Serilog.Sinks.Elasticsearch.ElasticsearchSinkOptions(new Uri(elasticUrl))
    {
        AutoRegisterTemplate = true,
        AutoRegisterTemplateVersion = Serilog.Sinks.Elasticsearch.AutoRegisterTemplateVersion.ESv8,
        ModifyConnectionSettings = conn =>
        {
            conn.BasicAuthentication(elasticUsername, elasticPassword);
            return conn;
        }
    });
}
else
{
    // Development: Elasticsearch without auth
    loggerConfig.WriteTo.Elasticsearch(new Serilog.Sinks.Elasticsearch.ElasticsearchSinkOptions(new Uri(elasticUrl))
    {
        AutoRegisterTemplate = true,
        AutoRegisterTemplateVersion = Serilog.Sinks.Elasticsearch.AutoRegisterTemplateVersion.ESv8
    });
}

Log.Logger = loggerConfig.CreateLogger();

builder.Host.UseSerilog();

// ── Services ───────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "VibeXLearn API v1",
        Version = "v1",
        Description = "DEPRECATED — Will be sunset on " +
            DateTimeOffset.UtcNow.AddMonths(6).ToString("yyyy-MM-dd") +
            ". Please migrate to v2.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "API Support",
            Email = "api@vibexlearn.com"
        }
    });

    options.SwaggerDoc("v2", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "VibeXLearn API v2",
        Version = "v2",
        Description = "Current stable version of the API."
    });
});

// OpenTelemetry distributed tracing & metrics
builder.Services.AddOpenTelemetryTracing(builder.Configuration);

// API Versioning
builder.Services.AddApiVersioningSetup();

// Layer DI
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIntegrations(builder.Configuration);

// Current user service (requires HttpContextAccessor)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Platform.Application.Common.Interfaces.ICurrentUserService,
    Platform.WebAPI.Services.HttpCurrentUserService>();

// Security
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddRateLimiting();
builder.Services.AddCorsPolicy(builder.Configuration);

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Platform.Persistence.Context.AppDbContext>(
        name: "database",
        tags: ["ready", "database"])
    .AddInfrastructureHealthChecks(builder.Configuration);

// AutoMapper
builder.Services.AddAutoMapper(
    _ => { },
    typeof(Program).Assembly,
    typeof(Platform.Application.Common.Results.Result).Assembly);

// FluentValidation
builder.Services.AddValidatorsFromAssembly(
    typeof(Platform.Application.Common.Results.Result).Assembly);

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Platform.Application.Common.Results.Result).Assembly);
    cfg.AddOpenBehavior(typeof(Platform.Application.Common.Behaviours.ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(Platform.Application.Common.Behaviours.QueryCachingBehavior<,>));
    cfg.AddOpenBehavior(typeof(Platform.Application.Common.Behaviours.CommandCacheInvalidationBehavior<,>));
});

// ── App ────────────────────────────────────────────────────────────────────
var app = builder.Build();

// Development: Auto migrations
if (app.Environment.IsDevelopment())
{
    await app.Services.ApplyMigrationsAsync();
}

// Security middlewares (sıra önemli!)
app.UseSecurityMiddlewares();

// API deprecation headers
app.UseMiddleware<Platform.Infrastructure.Middlewares.ApiDeprecationMiddleware>();

// X-Api-Version response header
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var path = context.Request.Path.Value;
        if (path is not null && path.Contains("/api/v", StringComparison.OrdinalIgnoreCase))
        {
            // Extract version from URL path
            var vIdx = path.IndexOf("/api/v", StringComparison.OrdinalIgnoreCase) + 6;
            var endIdx = path.IndexOf('/', vIdx);
            if (endIdx > vIdx)
            {
                context.Response.Headers["X-Api-Version"] = path[vIdx..endIdx];
            }
        }
        return Task.CompletedTask;
    });
    await next();
});

// Swagger
if (builder.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v2/swagger.json", "v2 (Current)");
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1 (Deprecated)");
    });
}

// HTTPS redirection
app.UseHttpsRedirection();

// CORS
app.UseCors("Frontend");

// Rate limiting
app.UseRateLimiter();

// Auth
app.UseAuthentication();
app.UseAuthorization();

// Map all Minimal API endpoints
app.RegisterAllEndpoints();

Log.Information("VibeXLearnPlatform API starting...");

app.Run();
