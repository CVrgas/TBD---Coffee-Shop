using System.Net.Http.Json;
using Application.Auth.Services;
using Application.Common.Abstractions.Envelope;
using Domain.User;
using Infrastructure.Persistence;
using Integration.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Auth;

public class AuthTests(IntegrationTestFactory factory) : BaseIntegrationTest(factory), IAsyncLifetime
{
    private readonly IntegrationTestFactory _factory = factory;
    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() =>  _factory.DisposeAsync();

    #region MyRegion

    private async Task<SeedResult> Seed(UserRole userRole = UserRole.Customer)
    {
        return await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher<User>>();
            var uniqueEmail = $"user{Guid.NewGuid()}@mail.com";

            // user
            var user = new User
            {
                FirstName = "user",
                LastName = "generic",
                Email = uniqueEmail,
                PasswordHash = "genericPassword123",
                Role = userRole,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                SecurityStamp = Guid.NewGuid().ToString(),
            };
            user.PasswordHash = hasher.HashPassword(user, user.PasswordHash);

            context.Users.Add(user);
            await context.SaveChangesAsync();
            
            return new SeedResult(user.Id, user.Email, user.FirstName, user.LastName);
        });
    }
    
    #endregion
    
    [Fact]
    public async Task GetMe_ValidRequest_ShouldReturnValidResponse()
    {
        // Arrange
        const UserRole userRole = UserRole.Customer;
        var seed = await Seed(userRole);
        SetUserContext(userId: seed.UserId, role: userRole);
        
        // Act
        var result = await Client.GetFromJsonAsync<Envelope<UserMeDto>>("/api/v1/auth/me");
        
        // Assert
        Assert.True(result?.IsSuccess);
        Assert.NotNull(result?.Data);
        Assert.Equal(result.Data.FirstName, seed.FirstName);
        Assert.Equal(result.Data.Email, seed.Email);
        Assert.Equal(result.Data.LastName, seed.LastName);
        
    }

    
}

internal sealed record SeedResult(int UserId, string Email, string FirstName, string LastName); 