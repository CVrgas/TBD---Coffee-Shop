using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Caching;

public static class RedisConfiguration
{
    public static IServiceCollection AddRedis(this IServiceCollection service, string redisConnectionString)
    {
        service.AddOutputCache(opts =>
        {
            // Base policy
            opts.AddBasePolicy(builder => builder.Expire(TimeSpan.FromSeconds(60)));
            
            // Catalog
            foreach (var cachePolicy in CachePolicy.List())
            {
                opts.AddPolicy(cachePolicy.Name, new PublicCachePolicy(cachePolicy.Name, cachePolicy.Expiration));
            }
        });
        
        service.AddStackExchangeRedisOutputCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "CoffeeShop_Output_";
        });

        return service;
    }
}
