using System.Diagnostics;
using Application.Auth.Dtos;
using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Security;
using Domain.Users;
using Domain.Users.Entities;
using Domain.Users.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Auth.Commands.Login;

public class LoginCommandHandler(
    IPasswordManager hasher, 
    IAppDbContext context, 
    IJwtTokenGenerator generator) : IRequestHandler<LoginCommand, Envelope<AuthResult>>
{
    private const string DummyPassword = "myStrongPassword123";
    
    public async Task<Envelope<AuthResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var watch = Stopwatch.StartNew();
        var email = new EmailAddress(request.Email);
        var user = await context.Users.Where(u => u.Email == email).FirstOrDefaultAsync(cancellationToken: cancellationToken);
        var userExist = user is not null;
        var hashToVerify = userExist ? user!.PasswordHash : hasher.HashPassword(DummyPassword);
        var userToVerify = user ?? User.CreateCustomer("dummy", "dummy", "dummy@dummy.com", hashToVerify);
        
        var passwordVerified = hasher.VerifyPassword(userToVerify, hashToVerify, request.Password);
        if (!userExist || !passwordVerified)
        {
            await ApplySecurityDelay(watch.ElapsedMilliseconds, cancellationToken);
            return Envelope<AuthResult>.Unauthorized("Invalid credentials");
        }
            
        var (authToken, expirationIn) = generator.GenerateJwtToken(user!);
        await ApplySecurityDelay(watch.ElapsedMilliseconds, cancellationToken);
        return Envelope<AuthResult>.Ok(new AuthResult(authToken, expirationIn));
    }
    
    private static async Task ApplySecurityDelay(long time, CancellationToken ct)
    {
        const int targetTime = 500;
        var delay = targetTime - (int)time;
        if (delay > 0) await Task.Delay(delay, ct);
    }
}