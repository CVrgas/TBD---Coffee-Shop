using Application.Common.Abstractions.Persistence.Paginated;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;

namespace Infrastructure.Caching;

/// <summary>
/// Forces caching even if the request is authenticated.
/// Use this for truly public data (like Product Catalogs) that doesn't change per-user.
/// </summary>
public sealed class PublicCachePolicy(string tag, TimeSpan expiration) : IOutputCachePolicy
{
    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        context.EnableOutputCaching = true;
        context.AllowCacheLookup = true;
        context.AllowCacheStorage = true;
        context.ResponseExpirationTimeSpan = expiration;

        var paginatedKeys = PaginatedQueriesValues().Concat(["page", "PageIndex", "PageSize", "size", "category", "q" ]).Distinct().ToArray();
        context.CacheVaryByRules.QueryKeys = new StringValues(paginatedKeys);
        
        context.Tags.Add(tag);

        return ValueTask.CompletedTask;
    }
    
    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken) 
        => ValueTask.CompletedTask;

    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken) 
        => ValueTask.CompletedTask;

    private IEnumerable<string> PaginatedQueriesValues()
    {
        return typeof(PaginatedRequest).GetProperties()
            .Select(p => p.Name)
            .AsEnumerable();
    }
}