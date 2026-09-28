using CCC.Domain.Types;

namespace CCC.Api;

// Malformed input fails while an endpoint builds its value objects. This turns
// that into a 400 with the value object's message, so endpoints don't need a
// check per field.
class InvalidValueFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (InvalidValueException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }
}
