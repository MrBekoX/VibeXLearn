using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Platform.WebAPI.Filters;

/// <summary>
/// Replaces version placeholders in paths (v{version}) with the current document version.
/// This ensures Swagger UI sends concrete URLs like /api/v1/... instead of /api/v{version}/...
/// </summary>
public sealed class SwaggerVersionPathDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        if (swaggerDoc.Paths.Count == 0)
            return;

        var version = (swaggerDoc.Info.Version ?? "v1").Trim();
        if (version.StartsWith('v') || version.StartsWith('V'))
            version = version[1..];

        var replacement = $"v{version}";
        var updatedPaths = new OpenApiPaths();

        foreach (var path in swaggerDoc.Paths)
        {
            var newPath = path.Key
                .Replace("v{version}", replacement, StringComparison.OrdinalIgnoreCase)
                .Replace("{version}", version, StringComparison.OrdinalIgnoreCase);

            // Remove obsolete "version" path parameter if present.
            foreach (var operation in path.Value.Operations.Values)
            {
                if (operation.Parameters is null || operation.Parameters.Count == 0)
                    continue;

                operation.Parameters = operation.Parameters
                    .Where(p => !(p.In == ParameterLocation.Path &&
                                  p.Name.Equals("version", StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            updatedPaths[newPath] = path.Value;
        }

        swaggerDoc.Paths = updatedPaths;
    }
}

