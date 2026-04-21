using Application.Common.Interfaces.Security;
using Domain.Users.Entities;
using Domain.Users.ValueObjects;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Security;

public class IdentityPasswordManager(IPasswordHasher<User> hasher) : IPasswordManager
{
    public PasswordHash HashPassword(string password)
    {
        var hashed = hasher.HashPassword(null!, password);
        return new PasswordHash(hashed);
    }

    public bool VerifyPassword(User user, string hashedPassword, string providedPassword)
    {
        var result = hasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
        return result != PasswordVerificationResult.Failed;
    }
}