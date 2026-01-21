namespace Application.Common.Interfaces.Security;

public interface IPasswordManager
{
    string HashPassword(Domain.User.User user, string password);
    bool VerifyPassword(Domain.User.User user, string hashedPassword, string providedPassword);
}