using Application.Common.Interfaces.Security;
using Domain.User;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Security;

public class IdentityPasswordManager(IPasswordHasher<User> hasher) : IPasswordManager
{
    public string HashPassword(User user, string password)
    {
        return hasher.HashPassword(user, password);
    }

    public bool VerifyPassword(User user, string hashedPassword, string providedPassword)
    {
        var result = hasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
        return result != PasswordVerificationResult.Failed;
    }
}