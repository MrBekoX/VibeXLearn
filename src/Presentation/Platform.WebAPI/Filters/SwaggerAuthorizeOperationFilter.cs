using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Platform.WebAPI.Filters;

/// <summary>
/// Adds bearer security requirement only for endpoints that require authorization.
/// </summary>
public sealed class SwaggerAuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        if (metadata is null || metadata.Count == 0)
            return;

        var hasAllowAnonymous = metadata.OfType<IAllowAnonymous>().Any();
        if (hasAllowAnonymous)
            return;

        var hasAuthorize = metadata.OfType<IAuthorizeData>().Any();
        if (!hasAuthorize)
            return;

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            }] = Array.Empty<string>()
        });

        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });
    }
}

