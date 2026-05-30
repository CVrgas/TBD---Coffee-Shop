namespace Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    (string token, int ExpirationInSeconds) GenerateJwtToken(Domain.Users.Entities.User user, TokenType type = TokenType.AuthToken);
}

public enum TokenType {AuthToken, RefreshToken}