using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CCC.Api;

// Mirrors RespectRequiredConstructorParameters, which requires nullable parameters
// too, unlike Swashbuckle's NonNullableReferenceTypesAsRequired.
class ConstructorParametersAsRequiredSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema { Properties: { Count: > 0 } properties } concrete)
            return;

        var constructors = context.Type.GetConstructors().Where(c => c.GetParameters().Length > 0).ToList();
        if (constructors is not [var constructor])
            return;

        foreach (var parameter in constructor.GetParameters().Where(p => !p.HasDefaultValue))
        {
            var name = properties.Keys.FirstOrDefault(key => string.Equals(key, parameter.Name, StringComparison.OrdinalIgnoreCase));
            if (name is null)
                continue;

            concrete.Required ??= new HashSet<string>();
            concrete.Required.Add(name);
        }
    }
}
