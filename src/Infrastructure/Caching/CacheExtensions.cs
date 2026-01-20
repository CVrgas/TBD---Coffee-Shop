using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Caching;

public static class CacheExtensions
{
    /// <summary>
    /// Invalidate tag on successful actions
    /// </summary>
    public static RouteHandlerBuilder InvalidateCacheTag(this RouteHandlerBuilder builder, string tag)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var result = await next(context);

            if (context.HttpContext.Response.StatusCode is < 200 or >= 300) return result;
            
            var cacheStore = context.HttpContext.RequestServices.GetRequiredService<IOutputCacheStore>();
            await cacheStore.EvictByTagAsync(tag, context.HttpContext.RequestAborted);

            return result;
        });
    }
}