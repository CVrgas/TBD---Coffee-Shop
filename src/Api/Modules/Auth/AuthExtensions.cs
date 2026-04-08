using Application.Auth.Commands.Login;
using Application.Auth.Commands.Register;
using Application.Auth.Interfaces;
using Application.Auth.Services;
using Application.Auth.Validators;
using FluentValidation;
using Infrastructure.Persistence.Abstractions;

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
        services.AddScoped<IAuthQueryService, AuthQueryService>();
        services.AddScoped<IAuthQueries, AuthQueries>();
        services.AddScoped<IValidator<LoginCommand>, LoginValidator>();
        services.AddScoped<IValidator<RegisterCommand>, RegisterValidator>();
        
        return services;
    }
}