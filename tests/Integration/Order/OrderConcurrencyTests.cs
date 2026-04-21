using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.Common;
using Application.Common.Interfaces.Security;
using Application.Orders.Commands.CreateOrder;
using Application.Orders.Dtos;
using Domain.Catalog;
using Domain.Inventory;
using Domain.Users.Entities;
using Domain.Users.Enum;
using Infrastructure.Persistence;
using Integration.Common;
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
            var context = services.GetRequiredService<ApplicationDbContext>();
            var hasher = services.GetRequiredService<IPasswordManager>();
            var user = role == UserRole.Customer
                ? User.CreateCustomer(
                    email: $"race_{Guid.NewGuid()}@test.com",
                    firstName: "Race",
                    lastName: "User",
                    passwordHash: hasher.HashPassword("password123!")
                )
                : User.CreateStaff(
                    email: $"race_{Guid.NewGuid()}@test.com",
                    firstName: "Race",
                    lastName: "User",
                    role: role,
                    passwordHash: hasher.HashPassword("password123!")
                );
            
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var productId = 0;
            if (createProduct)
            {
                var categoryGuid = Guid.NewGuid();
                var category = ProductCategory.Create(
                    name: "RaceCat" + categoryGuid,
                    code: "RC",
                    slug: "rc-" + categoryGuid
                );

                context.ProductCategories.Add(category);
                await context.SaveChangesAsync();

                var uniqueName = $"Coffee_{Guid.NewGuid()}";
                var product = Product.Create(
                    name: uniqueName,
                    sku: Utilities.GenerateSku(category.Code),
                    price: 10,
                    currencyCode: "USD",
                    categoryId: category.Id
                    );
                
                context.Products.Add(product);
                await context.SaveChangesAsync();
                
                var stockItem = StockItem.Initialize(product.Id);
                stockItem.ReceiveStock(stockQuantity);
                
                context.StockItems.Add(stockItem);
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
            var request = new CreateOrderCommand("USD", new List<OrderItemDto> { new(productId, 1) });
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
            var context = services.GetRequiredService<ApplicationDbContext>();
            var stockItem = await context.StockItems.FirstAsync(si => si.ProductId == productId);

            Assert.Equal(0, stockItem.AvailableQuantity);
            Assert.Equal(1, stockItem.ReservedQuantity);

            var orderCounts = await context.Orders.CountAsync();
            Assert.Equal(1, orderCounts);
        });
    }

    private async Task<HttpResponseMessage> SendConcurrencyRequest(int userId, CreateOrderCommand request)
    {
        var localClient = _factory.CreateClient();
        localClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme);
        
        localClient.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        localClient.DefaultRequestHeaders.Add(TestAuthHandler.UserRoleHeader, nameof(UserRole.Customer));
        
        var idempotencyKey = Guid.NewGuid().ToString();
        localClient.DefaultRequestHeaders.Add(TestAuthHandler.IdempotencyKey, idempotencyKey);
        
        return await localClient.PostAsJsonAsync("/api/v1/orders", request);
    }
}