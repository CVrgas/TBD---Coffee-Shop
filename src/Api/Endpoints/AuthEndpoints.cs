using Api.Middlewares;
using Application.Auth.Commands.Login;
using Application.Auth.Commands.Register;
using Application.Auth.Queries.GetMe;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

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

        group.MapPost("/register", async ([FromBody] RegisterCommand req, [FromServices] ISender sender, CancellationToken cancellationToken = default) => 
            await sender.Send(req, cancellationToken))
            .AddEndpointFilter(new ValidationFilter<RegisterCommand>())
            .WithSummary("User Registration")
            .WithName("Registration");
        
        group.MapPost("/login", async ([FromBody] LoginCommand req, [FromServices] ISender sender, CancellationToken cancellationToken = default) => 
            await sender.Send(req, cancellationToken))
            .AddEndpointFilter(new ValidationFilter<LoginCommand>())
            .WithSummary("User Login")
            .WithName("Login");

        group.MapGet("/me", async ([FromServices] ISender sender, CancellationToken cancellationToken = default) =>
                await sender.Send(new GetMeQuery(), cancellationToken))
            .RequireAuthorization()
            .WithSummary("Get Me")
            .WithName("GetMe");
        
        return endpoints;
    }
    
}