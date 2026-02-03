using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Application.Common.Interfaces;
using Domain.User;
using Infrastructure.Authentication;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Application.Common.Interfaces.Security;
using Application.Common.Interfaces.User;
using Infrastructure.Caching;
using Infrastructure.Idempotency;
using Infrastructure.Identity;
using Infrastructure.Integration;
using Infrastructure.Persistence.Seeding;
using Infrastructure.Security;
using Polly;
using Polly.Retry;
using StackExchange.Redis;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config) 
    {
        services.AddDbContext<ApplicationDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("DefaultConnection"), sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            }));

        services.AddRedis(config.GetConnectionString("RedisConnection") ?? "Not connection string");
        
        services.AddSingleton<IConnectionMultiplexer>( _ => 
            ConnectionMultiplexer.Connect(config.GetConnectionString("RedisConnection") ?? "Not connection string"));
        
        // OpenTelemetry
        services.AddOpenTelemetry()
            .ConfigureResource(res => res.AddService(config["OpenTelemetry:ServiceName"]!))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddOtlpExporter(otlp =>
                    {
                        otlp.Endpoint = new Uri(config["OpenTelemetry:OtlpExporterEndpoint"]!);
                    });
            });
        
        // Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(config)
            .CreateLogger();

        services.AddSerilog();


        // User Rate limiter
        services.AddDefaultRateLimiter(20, 120);
        
        // Polly retry builder
        services.AddResiliencePipeline("default-retry-pipeline", builder =>
        {
            builder.AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<DbUpdateConcurrencyException>(),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Constant,
                Delay = TimeSpan.FromSeconds(1),
                OnRetry = static _ => ValueTask.CompletedTask
            });
        });
        
        
        // Current user service
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddScoped(typeof(IReadRepository<,>), typeof(Repository<,>));
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped(typeof(IPasswordHasher<>), typeof(PasswordHasher<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIdempotencyProvider, RedisIdempotencyProvider>();
        services.AddScoped<IPasswordManager, IdentityPasswordManager>();
        
        services.AddScoped<IDataSeeder, DataSeeder>();
        
        services.Configure<JwtSettings>(config.GetSection("JwtSettings"));
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            var jwtSettings = config.GetSection("JwtSettings").Get<JwtSettings>();
    
            if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.Secret))
                throw new InvalidOperationException("JwtSettings:Secret is missing in appsettings.json");
            
            options.MapInboundClaims = false;
            
            options.TokenValidationParameters = new TokenValidationParameters()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtSettings.Secret)),
                
                ClockSkew = TimeSpan.Zero,
                RoleClaimType = "role"
                
            };
        });
        services.AddAuthorization(opts =>
        {
            opts.AddPolicy(AuthPolicyName.Customer, policy => policy.RequireRole(nameof(UserRole.Customer)));
            opts.AddPolicy(AuthPolicyName.Staff, policy => policy.RequireRole(nameof(UserRole.Staff)));
            opts.AddPolicy(AuthPolicyName.Admin, policy => policy.RequireRole(nameof(UserRole.Admin)));
            opts.AddPolicy(AuthPolicyName.ElevatedRights, policy => policy.RequireRole(nameof(UserRole.Staff), nameof(UserRole.Admin)));
            opts.AddPolicy(AuthPolicyName.RegisteredUser,  policy => policy.RequireRole(nameof(UserRole.Staff), nameof(UserRole.Admin), nameof(UserRole.Customer)));
            
        });
        
        return services;
    }
}