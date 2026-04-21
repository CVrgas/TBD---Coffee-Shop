using System.Net.Http.Json;
using Domain.Users.Enum;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Common;

public class BaseIntegrationTest : IClassFixture<IntegrationTestFactory>, IDisposable
{
    
    protected readonly HttpClient Client;
    private readonly IntegrationTestFactory _factory;
    
    public BaseIntegrationTest(IntegrationTestFactory factory)
    {
        _factory = factory;
        Client = factory.CreateClient();
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme);
    }

    /// <summary>
    /// Executes an action within an isolated service scope. 
    /// This ensures that the DbContext is clean and has no cached entities.
    /// </summary>
    protected async Task<T> ExecuteInScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        return await action.Invoke(scope.ServiceProvider);
    }
    
    /// <summary>
    /// Executes a non-returning action within an isolated service scope.
    /// </summary>
    protected async Task ExecuteInScopeAsync(Func<IServiceProvider, Task> action)
    {
        using var scope = _factory.Services.CreateScope();
        await action.Invoke(scope.ServiceProvider);
    }
    
    /// <summary>
    /// Sets the user identity for the Client's HTTP requests.
    /// </summary>
    protected void SetUserContext(int userId, UserRole role, string? idempotencyKey = null)
    {
        // Clear previous headers if they exist to avoid duplicates
        Client.DefaultRequestHeaders.Remove(TestAuthHandler.UserIdHeader);
        Client.DefaultRequestHeaders.Remove(TestAuthHandler.UserRoleHeader);
        Client.DefaultRequestHeaders.Remove(TestAuthHandler.IdempotencyKey);
        
        Client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        Client.DefaultRequestHeaders.Add(TestAuthHandler.UserRoleHeader, role.ToString());
        
        if(!string.IsNullOrWhiteSpace(idempotencyKey)) Client.DefaultRequestHeaders.Add(TestAuthHandler.IdempotencyKey, idempotencyKey);
    }
    
    /// <summary>
    /// Sends an HTTP POST request with an idempotency key header to simulate and test concurrent or repeated requests.
    /// </summary>
    /// <remarks>
    /// This method uses <see cref="HttpRequestMessage"/> to ensure the idempotency header is scoped 
    /// only to this specific request, preventing cross-test pollution.
    /// </remarks>
    /// <param name="url">The target API endpoint.</param>
    /// <param name="content">The request payload to be serialized as JSON.</param>
    /// <param name="key">The unique idempotency identifier (X-Idempotency-key).</param>
    /// <typeparam name="T">The type of the content being sent.</typeparam>
    /// <returns>A <see cref="HttpResponseMessage"/> containing the server's response.</returns>
    protected async Task<HttpResponseMessage> PostWithIdempotencyAsync<T>(string url, T content, string key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(content)
        };
        request.Headers.Add("X-Idempotency-key", key);
        return await Client.SendAsync(request);
    }
    
    
    public void Dispose()
    {
        Client.Dispose();
    }
}