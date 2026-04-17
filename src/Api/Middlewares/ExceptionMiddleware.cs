
using System.Diagnostics;
using Application.Common.Abstractions.Envelope;
using Microsoft.EntityFrameworkCore;

namespace Api.Middlewares;

/// <summary>
/// Middleware for handling exceptions globally.
/// </summary>
/// <param name="next">The next delegate in the request pipeline.</param>
/// <param name="logger">The logger instance.</param>
public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    /// <summary>
    /// Invokes the middleware to handle exceptions.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (Exception ex)
        {
            
            var env = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();

            var (statusCode, envelope) = MapException(ex, env.IsDevelopment());
            
            var requestId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
            
            var stampedEnvelope = envelope with { RequestId = requestId };
            
            if(statusCode >= 500) logger.LogError(ex, "Server error at {Path}", httpContext.Request.Path);
            else logger.LogWarning("Request error at {Path}: {Message}", httpContext.Request.Path, ex.Message);
            
            httpContext.Response.Headers["X-Request-ID"] = requestId;
            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/json";
            
            await httpContext.Response.WriteAsJsonAsync(stampedEnvelope);
        }
    }
    
    private static (int StatusCode, Envelope ErrorEnvelope) MapException(Exception ex, bool isDev)
    {
        return ex switch
        {
            DbUpdateConcurrencyException or { InnerException: DbUpdateConcurrencyException } 
                => (StatusCodes.Status409Conflict, 
                    Envelope.Conflict("Record was modified by someone else.").WithError("Concurrency", "Conflict detected")),

            ArgumentException argEx 
                => (StatusCodes.Status400BadRequest, 
                    Envelope.BadRequest("Invalid request.").WithError(argEx.ParamName ?? "Validation", argEx.Message)),

            UnauthorizedAccessException 
                => (StatusCodes.Status401Unauthorized, 
                    Envelope.Unauthorized()),
            
            InvalidOperationException
                => (StatusCodes.Status500InternalServerError, Envelope.InternalError(isDev ? ex.Message : "An unexpected error occurred.")),

            DbUpdateException or { InnerException: DbUpdateException } 
                => (StatusCodes.Status500InternalServerError, 
                    Envelope.InternalError()),

            _ => (StatusCodes.Status500InternalServerError, 
                Envelope.InternalError(isDev ? ex.Message : "An unexpected error occurred."))
        };
    }
}