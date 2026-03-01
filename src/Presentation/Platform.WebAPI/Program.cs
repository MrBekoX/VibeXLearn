using FluentValidation;
using Platform.Application.Extensions;
using Platform.Infrastructure.Extensions;
using Platform.Infrastructure.Middlewares;
using Platform.Integration.Extensions;
using Platform.Persistence.Extensions;
using Platform.WebAPI.Extensions;
using Platform.WebAPI.Filters;
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
if (string.IsNullOrWhiteSpace(elasticUsername) && !string.IsNullOrWhiteSpace(elasticPassword))
{
    elasticUsername = "elastic";
}

if (!string.IsNullOrWhiteSpace(elasticPassword))
{
    // Production: Elasticsearch with authentication
    loggerConfig.WriteTo.Elasticsearch(new Serilog.Sinks.Elasticsearch.ElasticsearchSinkOptions(new Uri(elasticUrl))
    {
        AutoRegisterTemplate = true,
        AutoRegisterTemplateVersion = Serilog.Sinks.Elasticsearch.AutoRegisterTemplateVersion.ESv8,
        ModifyConnectionSettings = conn =>
        {
            conn.BasicAuthentication(elasticUsername!, elasticPassword);
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
builder.Services.AddTransient<
    Microsoft.Extensions.Options.IConfigureOptions<Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions>,
    ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT token as: Bearer {your token}",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // Version parametresine otomatik varsayilan deger
    options.OperationFilter<SwaggerDefaultVersionFilter>();
    options.OperationFilter<SwaggerAuthorizeOperationFilter>();
    options.DocumentFilter<SwaggerVersionPathDocumentFilter>();
});

// OpenTelemetry distributed tracing & metrics
builder.Services.AddOpenTelemetryTracing(builder.Configuration);

// API Versioning
builder.Services.AddApiVersioningSetup();

// Layer DI
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIntegrations(builder.Configuration);
builder.Services.AddApplicationServices();

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
    var apiVersionDescriptionProvider =
        app.Services.GetRequiredService<Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider>();
    app.UseSwaggerUI(options =>
    {
        foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions
                     .OrderByDescending(d => d.ApiVersion))
        {
            var label = description.IsDeprecated
                ? $"{description.GroupName} (Deprecated)"
                : $"{description.GroupName} (Current)";
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", label);
        }
    });
}

// HTTPS redirection
var hasHttpsPort =
    !string.IsNullOrWhiteSpace(app.Configuration["HTTPS_PORTS"]) ||
    !string.IsNullOrWhiteSpace(app.Configuration["ASPNETCORE_HTTPS_PORT"]);
if (hasHttpsPort)
{
    app.UseHttpsRedirection();
}
else
{
    Log.Information("HTTPS redirection disabled because no HTTPS port is configured.");
}

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
