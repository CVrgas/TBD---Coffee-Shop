using Domain.Users.ValueObjects;

namespace Application.Common.Interfaces.Security;

public interface IPasswordManager
{
    PasswordHash HashPassword(string password);
    bool VerifyPassword(Domain.Users.Entities.User user, string hashedPassword, string providedPassword);
}