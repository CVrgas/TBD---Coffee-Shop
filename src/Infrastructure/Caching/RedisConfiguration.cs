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
            opts.AddPolicy(CachePolicies.Catalog, new PublicCachePolicy(CachePolicies.Catalog, TimeSpan.FromMinutes(5)));
        });
        
        service.AddStackExchangeRedisOutputCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "CoffeeShop_Output_";
        });

        return service;
    }
}
