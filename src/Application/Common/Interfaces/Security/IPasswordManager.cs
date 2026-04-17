using Domain.User;

namespace Application.Common.Interfaces.Security;

public interface IPasswordManager
{
    PasswordHash HashPassword(string password);
    bool VerifyPassword(Domain.User.User user, string hashedPassword, string providedPassword);
}