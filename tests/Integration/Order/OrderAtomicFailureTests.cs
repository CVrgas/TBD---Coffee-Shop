using System.Net;
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Order;

/// <summary>
/// This interceptor is our "Trojan horse". 
/// It allows everything to flow in memory but causes the database to explode at the end.
/// </summary>
public class DatabaseSaboteurInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        throw new DbUpdateException("Simulated atomic bomb in the database.");
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        throw new DbUpdateException("Simulated atomic bomb in the database.");
    }
}

public class OrderAtomicFailureTests(IntegrationTestFactory factory) : BaseIntegrationTest(factory), IAsyncLifetime
{
    private readonly IntegrationTestFactory _factory = factory;

    private sealed record SeedResult(int UserId, int ProductId, int CategoryId, int StockItemId);

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<SeedResult> Seed(int stockQuantity = 100)
    {
        return await ExecuteInScopeAsync(async services => {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher<User>>();
            
            var user = new User {
                Email = $"race_{Guid.NewGuid()}@test.com",
                FirstName = "Race", LastName = "User",
                PasswordHash = "strongPassword123",
                Role = UserRole.Customer,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            user.PasswordHash = hasher.HashPassword(user, user.PasswordHash);

            var category = new ProductCategory
            {
                Name = "failCat", Code = "FC", Slug = "fc-" + Guid.NewGuid()
            };
            
            var uniqueName = $"Coffee_{Guid.NewGuid()}";
            var product = new Product {
                Name = uniqueName,
                Sku = Utilities.GenerateSku(category.Code),
                Price = 10,
                Currency = new CurrencyCode("USD"),
                Category = category,
                StockItems = new List<Domain.Inventory.StockItem> { new() { IsActive = true } }
            };
            
            product.StockItems.First(si => si.IsActive).AdjustQuantity(stockQuantity);

            context.Users.Add(user);
            context.ProductCategories.Add(category);
            context.Products.Add(product);
            await context.SaveChangesAsync();
            
            return new SeedResult(user.Id, product.Id, category.Id, product.StockItems.First().Id);
        });
    }

    [Fact]
    public async Task AddOrder_WhenDatabaseFailsAtLastSecond_ShouldNotDeductStock()
    {
        // Arrange
        const int initialStock = 100;
        const int orderQuantity = 5;
        const string idempotencyKey = "myStrongIdempotencyKey";
        var seed = await Seed(stockQuantity: initialStock);

        using var sabotageFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<ApplicationDbContext>((sp, opts) => opts.AddInterceptors(new DatabaseSaboteurInterceptor()));
            });
        });

        var sabotagedClient = sabotageFactory.CreateClient();
        
        sabotagedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme);
        
        sabotagedClient.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, seed.UserId.ToString());
        sabotagedClient.DefaultRequestHeaders.Add(TestAuthHandler.UserRoleHeader, nameof(UserRole.Customer));
        sabotagedClient.DefaultRequestHeaders.Add("X-Idempotency-key", idempotencyKey);
        
        // Act
        var orderRequest = new OrderCreationDto("USD", new List<OrderItemDto> { new(seed.ProductId, orderQuantity) });
        var response = await sabotagedClient.PostAsJsonAsync("/api/v1/order/add", orderRequest);
        
        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            
            var stockItem = await context.StockItems
                .AsNoTracking()
                .Include(si => si.StockMovements)
                .FirstAsync(si => si.Id == seed.StockItemId);
            
            Assert.Equal(initialStock, stockItem.QuantityOnHand);
            Assert.Equal(0, stockItem.ReservedQuantity);
            
            Assert.Empty(stockItem.StockMovements);
            
            var orderExists = await context.Orders.AnyAsync(o => o.UserId == seed.UserId);
            Assert.False(orderExists);
        });
    }
}