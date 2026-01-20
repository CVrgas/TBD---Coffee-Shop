using System.Text.Json;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Api.Configuration;

/// <summary>
/// Configures health checks for the application.
/// </summary>
public static class ConfigureHealthCheck
{
    /// <summary>
    /// Adds custom health checks to the application builder.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The web application builder with health checks added.</returns>
    public static WebApplicationBuilder AddCustomHealthCheck(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags:["live"])
            .AddSqlServer(
                connectionString: builder.Configuration.GetConnectionString("DefaultConnection") 
                                  ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."),
                healthQuery: "SELECT 1;",
                name: "SqlServer",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready", "db", "sql", "sqlserver"]
            )
            .AddRedis(
                builder.Configuration.GetConnectionString("RedisConnection") 
                      ?? throw new InvalidOperationException("Connection string 'RedisConnection' not found."), 
                name: "redis",
                tags: ["ready", "cache", "redis"]);
        
        return builder;
    }
    
    /// <summary>
    /// Maps the custom health check endpoints.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application with health check endpoints mapped.</returns>
    public static WebApplication MapCustomHealthCheck(this WebApplication app)
    {

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live"),
            ResponseWriter = WriteResponse
        })
        .CacheOutput( x => x.NoCache());

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready"),
            ResponseWriter = WriteResponse
        })
        .CacheOutput(x => x.NoCache());
        
        return app;
    }
    
    private static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration,
            entries = report.Entries.Select(e => new
            {
                key = e.Key,
                value = new
                {
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.ToString(),
                    error = e.Value.Exception?.Message, 
                    data = e.Value.Data
                }
            }).ToDictionary(x => x.key, x => x.value)
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}