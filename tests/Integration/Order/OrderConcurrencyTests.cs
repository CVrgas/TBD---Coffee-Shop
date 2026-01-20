using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.Common;
using Application.Orders.Dtos;
using Domain.Base;
using Domain.Catalog;
using Domain.User;
using Infrastructure.Persistence;
using Integration.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Order;


public class OrderConcurrencyTests(IntegrationTestFactory factory) : BaseIntegrationTest(factory), IAsyncLifetime
{
    private sealed record SeedResult(int UserId, int ProductId);
    
    private readonly IntegrationTestFactory _factory = factory;

    #region Helpers

    
    private async Task<SeedResult> Seed(int stockQuantity = 100, UserRole role = UserRole.Customer, bool createProduct = true)
    {
        return await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<MyDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher<User>>();
            var user = new User
            {
                Email = $"race_{Guid.NewGuid()}@test.com",
                FirstName = "Race", LastName = "User",
                PasswordHash = "strongPassword123",
                Role = role,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            user.PasswordHash = hasher.HashPassword(user, user.PasswordHash);

            context.Users.Add(user);

            var productId = 0;
            if (createProduct)
            {
                var categoryGuid = Guid.NewGuid();
                var category = new ProductCategory
                {
                    Name = "RaceCat" + categoryGuid, Code = "RC", Slug = "rc-" + categoryGuid
                };

                context.ProductCategories.Add(category);

                var uniqueName = $"Coffee_{Guid.NewGuid()}";
                var product = new Product
                {
                    Name = uniqueName,
                    Sku = Utilities.GenerateSku(category.Code),
                    Price = 10,
                    Currency = new CurrencyCode("USD"),
                    Category = category,
                    StockItems = new List<Domain.Inventory.StockItem> { new() { IsActive = true } }
                };
                
                product.StockItems.First(si => si.IsActive).AdjustQuantity(stockQuantity);
                context.Products.Add(product);
                await context.SaveChangesAsync();
                productId = product.Id;
            }
            else
            {
                productId = await context.Products.Select(p => p.Id).FirstAsync();
                await context.SaveChangesAsync();
            }

            return new SeedResult(user.Id, productId);
        });

    }

    #endregion

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PlaceOrder_SimultaneousRequests_ShouldMaintainConsistency()
    {
        // Arrange
        const int totalRequests = 10;
        const int initialStock = 1;
        
        var userIds = new List<int>();
        var productId = 0;
        
        for (var i = 0; i < totalRequests; i++)
        {
            var seed = await Seed(stockQuantity: initialStock, createProduct: i == 0);
            userIds.Add(seed.UserId);
            if (i == 0) productId = seed.ProductId;
        }

        var tasks = new List<Task<HttpResponseMessage>>();
        
        // Act
        foreach (var userId in userIds)
        {
            var request = new OrderCreationDto("USD", new List<OrderItemDto> { new(productId, 1) });
            tasks.Add(Task.Run(async () => await SendConcurrencyRequest(userId, request)));
        }

        var responses = await Task.WhenAll(tasks);

        var successCount = responses.Count(r => r.IsSuccessStatusCode);
        var failedCount = responses.Count(r => !r.IsSuccessStatusCode);
        
        var messages = responses.Where(r => !r.IsSuccessStatusCode).Select( r => r.Content.ReadAsStringAsync().Result);
        
        // Assert
        Assert.Equal(1, successCount);
        Assert.Equal(totalRequests - 1, failedCount);

        await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<MyDbContext>();
            var stockItem = await context.StockItems.FirstAsync(si => si.ProductId == productId);

            Assert.Equal(0, stockItem.QuantityOnHand);
            Assert.Equal(1, stockItem.ReservedQuantity);

            var orderCounts = await context.Orders.CountAsync();
            Assert.Equal(1, orderCounts);
        });

    }

    private async Task<HttpResponseMessage> SendConcurrencyRequest(int userId, OrderCreationDto request)
    {
        var localClient = _factory.CreateClient();
        localClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme);
        
        localClient.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        localClient.DefaultRequestHeaders.Add(TestAuthHandler.UserRoleHeader, nameof(UserRole.Customer));
        
        var idempotencyKey = Guid.NewGuid().ToString();
        localClient.DefaultRequestHeaders.Add(TestAuthHandler.IdempotencyKey, idempotencyKey);
        
        return await localClient.PostAsJsonAsync("/api/v1/order/add", request);
    }
}