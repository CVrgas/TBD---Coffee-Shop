using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Application.Common.Interfaces;
using Domain.Users.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Infrastructure.Authentication;

public class JwtTokenGenerator(IOptions<JwtSettings> options) : IJwtTokenGenerator
{
    public (string Token, int ExpirationInSeconds) GenerateJwtToken(User user)
    {
        var expirationInSeconds = options.Value.ExpirationInSeconds;

        var claims = new List<Claim>()
        {
            new (JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new (JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)),
            new (JwtRegisteredClaimNames.Email, user.Email.Value),
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

    public (string TokenStr, string TokenHash, int ExpirationInSeconds) GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        
        var tokenStr = Convert.ToBase64String(randomNumber);
        var tokenHash = HashRefreshToken(tokenStr);
        var expiration = options.Value.RefreshExpirationInSeconds;

        return (tokenStr, tokenHash, expiration);
    }
    
    public string HashRefreshToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}