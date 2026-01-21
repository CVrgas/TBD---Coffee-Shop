using Api.Middlewares;
using Application.Auth.Dtos;
using Application.Auth.Interfaces;
using RegisterRequest = Application.Auth.Dtos.RegisterRequest;

namespace Api.Modules.Auth;

/// <summary>
/// Defines the endpoints for authentication operations.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Maps the authentication endpoints to the application's request pipeline.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder with authentication endpoints mapped.</returns>
    public static IEndpointRouteBuilder MapAuth(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth")
            .WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest req, IAuthService authService) => 
                await authService.RegisterAsync(req))
            .AddEndpointFilter(new ValidationFilter<RegisterRequest>())
            .WithSummary("User Registration")
            .WithName("Registration");
        
        group.MapPost("/login", async (LoginRequest req, IAuthService authService) =>
            await authService.LoginAsync(req))
            .AddEndpointFilter(new ValidationFilter<LoginRequest>())
            .WithSummary("User Login")
            .WithName("Login");

        group.MapGet("/me", async (IAuthService authService) =>
                await authService.GetMe())
            .RequireAuthorization()
            .WithSummary("Get Me")
            .WithName("GetMe");
        
        return endpoints;
    }
    
}