using Domain.Base.Entities;

namespace Domain.Users.Entities;

public class RefreshToken : Entity<int>
{
    public int UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken() { }

    public static RefreshToken Create(int userId, string tokenHash, int expiresInSeconds)
    {
        if(expiresInSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(expiresInSeconds), "Expiration must be greater than zero.");

        return new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Revoke()
    {
        RevokedAt = DateTimeOffset.UtcNow;
    }
}