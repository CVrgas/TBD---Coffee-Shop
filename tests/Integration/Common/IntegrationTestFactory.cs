using Infrastructure.Caching;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Respawn;
using StackExchange.Redis;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace Integration.Common;

public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime 
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();
    
    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7.0-alpine")
        .Build();
    
    private Respawner _respawner = null!;
    
    private string _connectionString = null!;

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();

        _connectionString = _sqlContainer.GetConnectionString();
        
        await _redisContainer.StartAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();


        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            TablesToIgnore = ["__EFMigrationsHistory"]
        });

    }

    public async Task DisposeAsync()
    {
        await _sqlContainer.StopAsync();
        await _redisContainer.StopAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        await _respawner.ResetAsync(connection);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            
            services.AddDbContext<ApplicationDbContext>(opts =>
                opts.UseSqlServer(_connectionString));
            
            services.RemoveAll(typeof(IConnectionMultiplexer));
            services.AddRedis(_redisContainer.GetConnectionString());
            
            services.AddSingleton<IConnectionMultiplexer>(_ => 
                ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString()));

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.AuthenticationScheme, _ => { });
            
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
            });
        });
        base.ConfigureWebHost(builder);
    }
}