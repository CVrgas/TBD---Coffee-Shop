using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Common.Interfaces;
using Domain.User;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Infrastructure.Authentication;

public class JwtTokenGenerator(IOptions<JwtSettings> options) : IJwtTokenGenerator
{

    public (string token, int ExpirationInSeconds) GenerateJwtToken(User user, TokenType type = TokenType.AuthToken)
    {
        
        var expirationInSeconds = type == TokenType.AuthToken 
            ? options.Value.ExpirationInSeconds 
            : options.Value.RefreshExpirationInSeconds;

        var claims = new List<Claim>()
        {
            new (JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new (JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)),
            new (JwtRegisteredClaimNames.Email, user.Email),
            new (JwtRegisteredClaimNames.GivenName, user.FirstName),
            new (JwtRegisteredClaimNames.FamilyName, user.LastName),
            new ("role", user.Role.ToString()),
            new ("security_stamp", user.SecurityStamp),
        };

        var secret = new SymmetricSecurityKey(Convert.FromBase64String(options.Value.Secret));

        var credentials = new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
        var descriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddSeconds(expirationInSeconds),
            SigningCredentials = credentials,
            Audience = options.Value.Audience,
            Issuer = options.Value.Issuer,
            IssuedAt = DateTime.UtcNow,

        };
        
        var securityToken = new JwtSecurityTokenHandler().CreateToken(descriptor);
        var token = new JwtSecurityTokenHandler().WriteToken(securityToken);
        
        return (token, expirationInSeconds);
    }
}