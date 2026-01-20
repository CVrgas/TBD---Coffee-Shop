using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Authentication;

public class JwtSettings
{
    [MinLength(32)]
    public string Secret { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public int ExpirationInSeconds { get; set; }
    public int RefreshExpirationInSeconds { get; set; }
}