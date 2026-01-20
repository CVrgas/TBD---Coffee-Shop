namespace Application.Common.Interfaces;

/// <summary>
/// handle idempotency 
/// </summary>
public interface IIdempotencyProvider
{
    Task<bool> TryReserveAsync(string key, TimeSpan expiry);
    Task SaveResponseAsync(string key, object response, TimeSpan expiry);
    Task<string?> GetResponseAsync(string key);
    Task RemoveAsync(string key);
}