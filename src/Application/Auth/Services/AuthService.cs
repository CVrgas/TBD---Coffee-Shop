using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq.Expressions;
using Application.Auth.Dtos;
using Application.Auth.Interfaces;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Security;
using Application.Common.Interfaces.User;
using Domain.User;

namespace Application.Auth.Services;

public sealed class UserMeDto
{
    [NotMapped]
    public bool IsActive { get; init; }
    public string Email { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string Role { get; init; } = null!;
};

public class AuthService(
    IPasswordManager hasher, 
    IRepository<User, int> repository, 
    IJwtTokenGenerator generator,
    ICurrentUserService userContext,
    IUnitOfWork uOw) : IAuthService
{
    private readonly string _dummyPassword = "myStrongPassword123";
    public async Task<Envelope<RegisterResult>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var emailExist = await EmailExistAsync(request.Email, ct);
        if (emailExist) return Envelope<RegisterResult>.BadRequest("Email already exist");

        var user = new User
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = UserRole.Customer,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString(),
            IsActive = true,
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        await repository.Create(user);
        await uOw.SaveChangesAsync(ct);
        return Envelope<RegisterResult>.Ok(new RegisterResult(user.Id));
    }

    public async Task<Envelope<AuthResult>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var watch = Stopwatch.StartNew();
        var user = await repository.GetAsync(new ExistEmailSpec(request.Email), ct: ct);
        var userExist = user is not null;
        user ??= new User { PasswordHash = _dummyPassword};
        
        var passwordVerified = hasher.VerifyPassword(user, user.PasswordHash, request.Password);
        if (!userExist || !passwordVerified)
        {
            await ApplySecurityDelay(watch.ElapsedMilliseconds, ct);
            return Envelope<AuthResult>.Unauthorized("Invalid credentials");
        }
            
        var (authToken, expirationIn) = generator.GenerateJwtToken(user);
        await ApplySecurityDelay(watch.ElapsedMilliseconds, ct);
        return Envelope<AuthResult>.Ok(new AuthResult(authToken, expirationIn));
    }

    public async Task<Envelope<UserMeDto>> GetMe()
    {
        var userId = userContext.RequiredUserId;
        Expression<Func<User, UserMeDto>> selector = u => new UserMeDto
        {
            IsActive = u.IsActive,
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Role = u.Role.ToString(),
        };
        var user = await repository.GetAsync(new UserMeSpec(userId), selector: selector);
        return user switch
        {
            { IsActive: false } => Envelope<UserMeDto>.Forbidden(),
            not null => Envelope<UserMeDto>.Ok(user),
            _ => Envelope<UserMeDto>.NotFound("User not found")
        };

    }
    
    private async Task ApplySecurityDelay(long time, CancellationToken ct)
    {
        const int targetTime = 500;
        var delay = targetTime - (int)time;
        if (delay > 0) await Task.Delay(delay, ct);
    }
    
    private async Task<bool> EmailExistAsync(string email, CancellationToken ct = default) =>
        await repository.ExistsAsync(new ExistEmailSpec(email), ct: ct);
}

public class UserMeSpec(int id): Specification<User>(u => u.Id == id);
public class ExistEmailSpec(string email): Specification<User>(u => u.IsActive && email == u.Email);