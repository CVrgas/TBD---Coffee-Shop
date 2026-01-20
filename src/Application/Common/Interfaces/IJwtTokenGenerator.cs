namespace Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    (string token, int ExpirationInSeconds) GenerateJwtToken(Domain.User.User user, TokenType type = TokenType.AuthToken);
}

public enum TokenType {AuthToken, RefreshToken}