using Application.Auth.Dtos;
using Application.Auth.Interfaces;
using Application.Auth.Services;
using Application.Auth.Validators;
using FluentValidation;

namespace Api.Modules.Auth;

/// <summary>
/// Extension methods for setting up authentication services in the dependency injection container.
/// </summary>
public static class AuthExtensions
{
    /// <summary>
    /// Adds authentication-related services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection with authentication services added.</returns>
    public static IServiceCollection AddAuth(this IServiceCollection services)
    {
        services.AddScoped<IAuthService,  AuthService>();
        
        services.AddScoped<IValidator<LoginRequest>, LoginValidator>();
        services.AddScoped<IValidator<RegisterRequest>, RegisterValidator>();
        
        return services;
    }
}