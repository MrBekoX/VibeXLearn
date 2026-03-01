using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Platform.WebAPI.Extensions;

/// <summary>
/// Configures Swagger docs based on discovered API versions.
/// </summary>
public sealed class ConfigureSwaggerOptions(
    IApiVersionDescriptionProvider provider) : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            var info = new OpenApiInfo
            {
                Title = "VibeXLearn API",
                Version = description.GroupName,
                Description = description.IsDeprecated
                    ? "Deprecated API version."
                    : "Current stable version of the API."
            };

            options.SwaggerDoc(description.GroupName, info);
        }
    }
}

