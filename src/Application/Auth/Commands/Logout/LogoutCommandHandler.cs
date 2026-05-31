using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Auth.Commands.Logout;

public class LogoutCommandHandler(
    IAppDbContext context,
    IJwtTokenGenerator generator) : IRequestHandler<LogoutCommand, Envelope>
{
    public async Task<Envelope> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = generator.HashRefreshToken(request.RefreshToken);
        
        var existingToken = await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (existingToken is null || !existingToken.IsActive) return Envelope.Ok();
        
        existingToken.Revoke();
        await context.SaveChangesAsync(cancellationToken);

        return Envelope.Ok();
    }
}
// YRqV65hxJ10ky2PFeAszQsECLgio7/4oRdXPsOeYWLk=