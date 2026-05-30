namespace Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, int ExpirationInSeconds) GenerateJwtToken(Domain.Users.Entities.User user);
    
    (string TokenStr, string TokenHash, int ExpirationInSeconds) GenerateRefreshToken();
    
    string HashRefreshToken(string token);
}

public enum TokenType { AuthToken, RefreshToken }