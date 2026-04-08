using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Authentication;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";
    
    [MinLength(32)]
    [Required]
    public required string Secret { get; init; }
    
    [Required]
    public required string Issuer { get; init; }
    
    [Required]
    public required string Audience { get; init; }
    
    [Range(1, int.MaxValue)]
    public int ExpirationInSeconds { get; init; }
    
    [Range(1, int.MaxValue)]
    public int RefreshExpirationInSeconds { get; init; }
}