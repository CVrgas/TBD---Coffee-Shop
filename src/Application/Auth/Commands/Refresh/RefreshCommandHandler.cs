using Application.Auth.Dtos;
using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces;
using Domain.Users.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Auth.Commands.Refresh;


public class RefreshCommandHandler(
    IAppDbContext context,
    IJwtTokenGenerator generator) : IRequestHandler<RefreshCommand, Envelope<AuthResult>>
{
    public async Task<Envelope<AuthResult>> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = generator.HashRefreshToken(request.RefreshToken);
        
        var existingToken = await context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (existingToken is null)
            return Envelope<AuthResult>.Unauthorized("Invalid token.");

        if (existingToken.IsRevoked)
        {
            var allUserTokens = await context.RefreshTokens
                .Where(rt => rt.UserId == existingToken.UserId)
                .ToListAsync(cancellationToken);
            
            foreach(var token in allUserTokens) token.Revoke();
            await context.SaveChangesAsync(cancellationToken);
            
            return Envelope<AuthResult>.Unauthorized("Compromised token detected. Session terminated.");
        }

        if (existingToken.IsExpired)
            return Envelope<AuthResult>.Unauthorized("Token expired.");

        var user = await context.Users.FindAsync([existingToken.UserId], cancellationToken);
        if (user is null || !user.IsActive)
            return Envelope<AuthResult>.Unauthorized("User inactive or not found.");

        existingToken.Revoke();
        
        var (newJwt, newExpirationIn) = generator.GenerateJwtToken(user);
        
        // Settings injection removed. Dependency inversion restored.
        var (newRefreshTokenStr, newRefreshTokenHash, refreshExp) = generator.GenerateRefreshToken();
        
        var newRefreshTokenEntity = RefreshToken.Create(user.Id, newRefreshTokenHash, refreshExp);
        context.RefreshTokens.Add(newRefreshTokenEntity);
        
        await context.SaveChangesAsync(cancellationToken);

        return Envelope<AuthResult>.Ok(new AuthResult(newJwt, newExpirationIn, newRefreshTokenStr));
    }
}