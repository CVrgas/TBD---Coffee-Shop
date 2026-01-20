using System.Text.Json;
using Application.Common.Interfaces;

namespace Api.Common.Idempotency;

public class IdempotentEndpointFilter(int expiryMinutes = 60) : IEndpointFilter
{
    private const string HeaderName = "X-Idempotency-key";
    
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.HttpContext.Request;
        if (!request.Headers.TryGetValue(HeaderName, out var key) || string.IsNullOrWhiteSpace(key))
        {
            return Application.Common.Abstractions.Envelope.Envelope.BadRequest("Missing X-Idempotency-Key header.");
        }

        var provider = context.HttpContext.RequestServices.GetRequiredService<IIdempotencyProvider>();
        var expiry = TimeSpan.FromMinutes(expiryMinutes);

        if (!await provider.TryReserveAsync(key!, expiry))
        {
            var previousResponse = await provider.GetResponseAsync(key!);
            return previousResponse == "PROCESSING"
                ? Application.Common.Abstractions.Envelope.Envelope.Conflict("Request is already being processed.") 
                : JsonSerializer.Deserialize<object>(previousResponse!);
        }

        try
        {
            var result = await next(context);
            await provider.SaveResponseAsync(key!, result!, expiry);
            return result;
        }
        catch (Exception)
        {
            await provider.RemoveAsync(key!);
            throw;
        }
    }
}