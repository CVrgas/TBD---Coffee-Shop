using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.RateLimiting;
using Polly.Retry;

namespace Infrastructure.Persistence;

public static class DefaultRateLimiter
{
    public static IServiceCollection AddDefaultRateLimiter(this IServiceCollection services,
        int limit = 10, int windowSeconds = 20, string policyName = "per-user")
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // policy configuration
            options.AddPolicy(policyName, httpContext =>
            {
                var key = GetPartitionKey(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: key,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limit,
                        Window = TimeSpan.FromSeconds(windowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            // On reject response
            options.OnRejected = (ctx, _) =>
            {
                var retry = ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                    ? retryAfter.TotalSeconds
                    : 5;

                var retryStr = Math.Ceiling(retry).ToString(CultureInfo.InvariantCulture);

                ctx.HttpContext.Response.Headers.RetryAfter = retryStr;
                ctx.HttpContext.Response.Headers["RateLimit-Policy"] = $"{limit};w={windowSeconds}";

                return ValueTask.CompletedTask;
            };
        });

        return services;
    }

    private static string GetPartitionKey(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userId =
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                context.User.FindFirst("sub")?.Value ??
                context.User.Identity?.Name;

            if (!string.IsNullOrWhiteSpace(userId))
                return $"usr:{userId}";
        }
        
        if (context.Request.Headers.TryGetValue("X-Api-Key", out var key) && !string.IsNullOrWhiteSpace(key)) 
            return $"key:{key}";
        
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
        var ua = context.Request.Headers.UserAgent.ToString();
        var uaHash = ua.GetHashCode(StringComparison.OrdinalIgnoreCase).ToString("X");
        return $"ipua:{ip}:{uaHash}";
    }
}