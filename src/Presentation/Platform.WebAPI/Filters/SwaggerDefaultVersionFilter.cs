using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Platform.WebAPI.Filters;

/// <summary>
/// Swagger UI'da "version" path parameter'ına otomatik varsayılan değer atar.
/// </summary>
public class SwaggerDefaultVersionFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters == null)
            return;

        foreach (var parameter in operation.Parameters)
        {
            if (parameter.Name.Equals("version", StringComparison.OrdinalIgnoreCase) &&
                parameter.In == ParameterLocation.Path)
            {
                parameter.Schema.Default = new Microsoft.OpenApi.Any.OpenApiString("1");
                parameter.Description ??= "API version (default: v1)";
            }
        }
    }
}
