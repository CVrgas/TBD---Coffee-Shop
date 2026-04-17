using Application.Common.Abstractions.Envelope;
using FluentValidation;

namespace Api.Middlewares;

/// <summary>
/// A filter that validates the request body using FluentValidation.
/// </summary>
/// <typeparam name="T">The type of the request body to validate.</typeparam>
public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    /// <summary>
    /// Invokes the validation logic.
    /// </summary>
    /// <param name="context">The context for the filter invocation.</param>
    /// <param name="next">The delegate to invoke the next filter or endpoint.</param>
    /// <returns>The result of the validation or the result of the next filter/endpoint.</returns>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null)
        {
            return await next(context);
        }

        var arg = context.Arguments.FirstOrDefault(a => a?.GetType() == typeof(T)) as T;
        if (arg is null)
        {
            return Envelope.BadRequest("The request body is missing or invalid.");
        }

        var validationResult = await validator.ValidateAsync(arg);
        
        if (validationResult.IsValid) return await next(context);
        
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName).Select(g =>
                new KeyValuePair<string, IEnumerable<string>>(g.Key, g.Select(e => e.ErrorMessage))).ToList();
            
        return
            Envelope.BadRequest()
                .WithError(errors);
    }
}