using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Common;
using Application.Common.Abstractions.Envelope;
using Application.Orders.Dtos;
using Domain.Base;
using Domain.Catalog;
using Domain.Inventory;
using Domain.User;
using Infrastructure.Persistence;
using Integration.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Order;

public sealed record SeededIds(int UserId, int ProductId, int CategoryId, int StockItemId);

public class OrderTests(IntegrationTestFactory factory) : BaseIntegrationTest(factory), IAsyncLifetime
{
    private readonly IntegrationTestFactory _factory = factory;

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;
    
    #region Helpers
    private async Task<SeededIds> Seed(decimal price = 10.00m, int stockQuantity = 100, UserRole userRole = UserRole.Customer)
    {
        return await ExecuteInScopeAsync(async services =>
        {
            // Context
            var context = services.GetRequiredService<ApplicationDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher<User>>();
            var uniqueEmail = $"user{Guid.NewGuid()}@mail.com";
            
            // user
            var user = new User {
                FirstName = "user", 
                LastName = "generic",
                Email = uniqueEmail,
                PasswordHash = "genericPassword123",
                Role = userRole,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString(),
            };
            user.PasswordHash = hasher.HashPassword(user, user.PasswordHash);
            
            // product category
            var category = new ProductCategory {
                Name = "Coffee",
                Slug = ("coffee-" + Guid.NewGuid()).Slugify(),
                Code = "Coffee".ToUpperInvariant().Replace(" ", ""),
                Description = "whole coffee",
            };

            // product
            var uniqueName = $"Coffee_{Guid.NewGuid()}";
            var product = new Product {
                Name = uniqueName,
                Sku = Utilities.GenerateSku(category.Code),
                Price = price,
                Currency = new CurrencyCode("USD"),
                Description = "best colombian coffee",
                Category = category,
                StockItems = new List<StockItem>{new() { IsActive = true, }},
            };
            
            // add quantity on stock.
            product.StockItems.First(si => si.IsActive).AdjustQuantity(stockQuantity);

            context.Users.Add(user);
            context.ProductCategories.Add(category);
            context.Products.Add(product);
            await context.SaveChangesAsync();
            
            return new SeededIds(user.Id, product.Id, category.Id, product.StockItems.First(si => si.IsActive).Id);
        });
    }
    
    #endregion

    [Fact]
    public async Task PlaceOrder_ValidRequest_ShouldCreateOrderAndDeductStock()
    {
        const decimal productPrice = 10.00m;
        const int orderQuantity = 2;
        const int initialStock = 100;
        const UserRole userRole = UserRole.Admin;
        const string idempotencyKey = "myStrongIdempotencyKey";
        
        // ARRANGE
        var seeded = await Seed(price: productPrice, stockQuantity: initialStock, userRole: userRole);
        SetUserContext(seeded.UserId, userRole, idempotencyKey); // Set user
        var order = new OrderCreationDto("USD", new List<OrderItemDto> { new (seeded.ProductId, orderQuantity) });

        // ACT
        var response = await Client.PostAsJsonAsync("/api/v1/order/add", order);
        var result = await response.Content.ReadFromJsonAsync<Envelope>();

        // ASSERT
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        
        await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            var dbOrder = await context.Orders
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync(o => o.UserId == seeded.UserId);

            Assert.NotNull(dbOrder);
            Assert.Equal((productPrice * orderQuantity) * 1.18m, dbOrder.Total);
            
            var stockItem = await context.StockItems
                .AsNoTracking()
                .Include(si => si.StockMovements)
                .FirstAsync(si => si.Id == seeded.StockItemId);
            
            // 3. Verify StockItem.QuantityOnHand is now 98 (100 - 2).
            Assert.Equal(orderQuantity, stockItem.ReservedQuantity);
            Assert.Equal(initialStock - orderQuantity, stockItem.QuantityOnHand);
            
            // 4. Verify a StockMovement record was created with Reason = "Order".
            Assert.Contains(stockItem.StockMovements, 
                sm => sm.Reason == StockMovementReason.Reserve && sm.ReferenceId == dbOrder.OrderNumber);

        });
    }

    [Fact]
    public async Task PlaceOrder_InsufficientStock_ShouldReturnBadRequest()
    {
        // ARRANGE
        const int stockQuantity = 5;
        const int orderQuantity = 10;
        const UserRole userRole = UserRole.Customer;
        
        var seed = await Seed(stockQuantity: stockQuantity, userRole: userRole);
        SetUserContext(seed.UserId, userRole);

        // ACT
        var order = new OrderCreationDto("USD", new List<OrderItemDto>{ new (seed.ProductId, orderQuantity) });
        var response = await Client.PostAsJsonAsync("/api/v1/order/add", order);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // ASSERT
        Assert.NotNull(problem);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
        Assert.Equal("Bad Request", problem.Title);
        
        if (problem.Extensions.TryGetValue("errors", out var errorsObj))
        {
            var errorsJson = (JsonElement)errorsObj!;
            var errors = errorsJson.Deserialize<Dictionary<string, string[]>>();

            Assert.Contains(errors!, e => e.Key.Contains("Stock"));
            Assert.Equal("Not enough stock.", errors!["Stock"][0]);
        }
        
        await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var dbOrder = await context.Orders.FirstOrDefaultAsync(o => o.UserId == seed.UserId);
            Assert.Null(dbOrder);
        });
    }
    
    [Fact]
    public async Task GetOrders_CustomerRole_ShouldOnlyReturnOwnOrders()
    {
        // ARRANGE
        var seed = await Seed();
        SetUserContext(seed.UserId, UserRole.Customer, Guid.NewGuid().ToString());
        await Client.PostAsJsonAsync("/api/v1/order/add", new OrderCreationDto("USD", new List<OrderItemDto> { new (seed.ProductId, 10) }));
        
        var seed2 = await Seed();
        SetUserContext(seed2.UserId, UserRole.Customer, Guid.NewGuid().ToString());
        await Client.PostAsJsonAsync("/api/v1/order/add", new OrderCreationDto("USD", new List<OrderItemDto> { new (seed2.ProductId, 10) }));
       
        SetUserContext(seed.UserId, UserRole.Customer, Guid.NewGuid().ToString());
        
        // ACT
        var response = await Client.GetAsync("/api/v1/order/all");
        Assert.True(response.IsSuccessStatusCode);
        var result = await response.Content.ReadFromJsonAsync<Envelope<List<OrderDto>>>();

        // ASSERT
        Assert.NotNull(result?.Data);
        Assert.Single(result.Data);
    }

    [Fact]
    public async Task CancelOrder_ValidOrder_ShouldRestoreStock()
    {
        // ARRANGE
        const int initialStock = 100;
        const int orderQuantity = 5;
        const UserRole role = UserRole.Customer;
        
        var seed = await Seed( stockQuantity: initialStock, userRole: role);
        SetUserContext(seed.UserId, role, Guid.NewGuid().ToString());
        
        var order = new OrderCreationDto("USD", new List<OrderItemDto> { new (seed.ProductId, orderQuantity) });
        await Client.PostAsJsonAsync("/api/v1/order/add", order);
        
        var orderNumber = await ExecuteInScopeAsync(async service => {
            return await service.GetRequiredService<ApplicationDbContext>()
                .Orders.Where(o => o.UserId == seed.UserId)
                .Select(o => o.OrderNumber).FirstOrDefaultAsync();
        });

        await ExecuteInScopeAsync(async service => {
            var context = service.GetRequiredService<ApplicationDbContext>();
            var stockItems = await context.StockItems
                .Where(si => si.ProductId == seed.ProductId)
                .ToListAsync();

            var stockCount = stockItems.Sum(si => si.QuantityOnHand);
            var reservedCount = stockItems.Sum(si => si.ReservedQuantity);
            
            // 2. Verify current stock is 95.
            Assert.Equal(initialStock - orderQuantity, stockCount);
            Assert.Equal(orderQuantity, reservedCount);
        });

        // ACT
        var response = await Client.PostAsJsonAsync("/api/v1/order/cancel", orderNumber);

        // ASSERT
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await ExecuteInScopeAsync(async service => {
            var context = service.GetRequiredService<ApplicationDbContext>();
            
            var dbOrder = context.Orders.FirstOrDefault(o => o.OrderNumber == orderNumber);
            Assert.NotNull(dbOrder);
            Assert.Equal(OrderStatus.Cancelled, dbOrder.Status);

            var stockItem = await context.StockItems
                .Include(si => si.StockMovements)
                .Where(si => si.Id == seed.StockItemId)
                .FirstAsync();
            
            Assert.Equal(initialStock, stockItem.QuantityOnHand);
            Assert.Equal(0, stockItem.ReservedQuantity);
            Assert.Contains(stockItem.StockMovements, mv=> mv.ReferenceId == dbOrder.OrderNumber && mv.Reason == StockMovementReason.Restore);
        });
    }
}