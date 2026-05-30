using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.Common;
using Application.Common.Interfaces.Security;
using Application.Orders.Commands.CreateOrder;
using Application.Orders.Dtos;
using Domain.Base.Enum;
using Domain.Catalog;
using Domain.Inventory;
using Domain.Users.Entities;
using Domain.Users.Enum;
using Infrastructure.Persistence;
using Integration.Common;
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
            var hasher = services.GetRequiredService<IPasswordManager>();
            
            var user = User.CreateCustomer(
                email: $"race_{Guid.NewGuid()}@test.com",
                firstName: "Race", 
                lastName: "User",
                passwordHash: hasher.HashPassword("password123!")
                );
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var category = ProductCategory.Create(
                name: "failCat",
                code: "FC",
                slug: "fc-" + Guid.NewGuid()
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
            
            return new SeedResult(user.Id, product.Id, category.Id, stockItem.Id);
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
        var orderRequest = new CreateOrderCommand("USD", new List<OrderItemDto> { new(seed.ProductId, orderQuantity) });
        var response = await sabotagedClient.PostAsJsonAsync("/api/v1/orders", orderRequest);
        
        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            
            var stockItem = await context.StockItems
                .AsNoTracking()
                .Include(si => si.Movements)
                .FirstAsync(si => si.Id == seed.StockItemId);
            
            Assert.Equal(initialStock, stockItem.QuantityOnHand);
            Assert.Equal(0, stockItem.ReservedQuantity);
            
            // Verify items were not reserved
            Assert.DoesNotContain(stockItem.Movements, s => s.Id == seed.StockItemId && s.Reason == StockMovementReason.Reserve);
            
            var orderExists = await context.Orders.AnyAsync(o => o.UserId == seed.UserId);
            Assert.False(orderExists);
        });
    }
}