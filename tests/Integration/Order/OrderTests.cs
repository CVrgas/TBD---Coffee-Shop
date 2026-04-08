using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Common;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Orders.Commands.CancelOrder;
using Application.Orders.Commands.CreateOrder;
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
            var user = userRole == UserRole.Customer
                ? User.CreateCustomer(
                    firstName: "user", 
                    lastName: "generic",
                    email: uniqueEmail
                )
                : User.CreateStaff(
                    firstName: "user", 
                    lastName: "generic",
                    email: uniqueEmail,
                    role: userRole
                );

            user.SetPassword(hasher.HashPassword(user, user.PasswordHash));
            context.Users.Add(user);
            
            // product category
            var category = ProductCategory.Create(
                name: "Coffee",
                slug: ("coffee-" + Guid.NewGuid()).Slugify(),
                code: "Coffee".ToUpperInvariant().Replace(" ", ""),
                description: "whole coffee"
            );
            context.ProductCategories.Add(category);
            await context.SaveChangesAsync();

            // product
            var uniqueName = $"Coffee_{Guid.NewGuid()}";
            var product = Product.Create(
                name: uniqueName,
                sku: Utilities.GenerateSku(category.Code),
                price: price,
                currencyCode: "USD",
                description: "best colombian coffee",
                categoryId: category.Id
            );
            context.Products.Add(product);
            await context.SaveChangesAsync();
            
            // add quantity on stock.
            var stockItem = StockItem.Initialize(product.Id);
            stockItem.ReceiveStock(stockQuantity);
            
            context.StockItems.Add(stockItem);
            await context.SaveChangesAsync();
            
            return new SeededIds(user.Id, product.Id, category.Id, stockItem.Id);
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
        var order = new CreateOrderCommand("USD", new List<OrderItemDto> { new (seeded.ProductId, orderQuantity) });

        // ACT
        var response = await PostWithIdempotencyAsync("/api/v1/orders", order, Guid.NewGuid().ToString());
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
                .Include(si => si.Movements)
                .FirstAsync(si => si.Id == seeded.StockItemId);
            
            // check quantity on stock is reserved.
            Assert.Equal(orderQuantity, stockItem.ReservedQuantity);
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
        var order = new CreateOrderCommand("USD", new List<OrderItemDto>{ new (seed.ProductId, orderQuantity) });
        var response = await Client.PostAsJsonAsync("/api/v1/orders", order);
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
        await PostWithIdempotencyAsync("/api/v1/orders", new CreateOrderCommand("USD", new List<OrderItemDto> { new (seed.ProductId, 10) }), Guid.NewGuid().ToString());
        
        var seed2 = await Seed();
        SetUserContext(seed2.UserId, UserRole.Customer, Guid.NewGuid().ToString());
        await PostWithIdempotencyAsync("/api/v1/orders", new CreateOrderCommand("USD", new List<OrderItemDto> { new (seed2.ProductId, 10) }), Guid.NewGuid().ToString());
       
        SetUserContext(seed.UserId, UserRole.Customer, Guid.NewGuid().ToString());
        
        // ACT
        var response = await Client.GetAsync("/api/v1/orders");
        Assert.True(response.IsSuccessStatusCode);
        var result = await response.Content.ReadFromJsonAsync<Envelope<Paginated<OrderDto>>>();

        // ASSERT
        Assert.NotNull(result?.Data);
        Assert.Single(result.Data.Entities);
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
        
        var order = new CreateOrderCommand("USD", new List<OrderItemDto> { new (seed.ProductId, orderQuantity) });
        await Client.PostAsJsonAsync("/api/v1/orders/", order);
        
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

            var reservedCount = stockItems.Sum(si => si.ReservedQuantity);
            
            // Verify item was reserved.
            Assert.Equal(orderQuantity, reservedCount);
        });

        // ACT
        var response = await Client.PostAsJsonAsync($"/api/v1/orders/{orderNumber}/cancel", new CancelOrderCommand(OrderNumber: orderNumber!));

        // ASSERT
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await ExecuteInScopeAsync(async service => {
            var context = service.GetRequiredService<ApplicationDbContext>();
            
            var dbOrder = context.Orders.FirstOrDefault(o => o.OrderNumber == orderNumber);
            Assert.NotNull(dbOrder);
            Assert.Equal(OrderStatus.Cancelled, dbOrder.Status);

            var stockItem = await context.StockItems
                .Include(si => si.Movements)
                .Where(si => si.Id == seed.StockItemId)
                .FirstAsync();
            
            Assert.Equal(initialStock, stockItem.QuantityOnHand);
            Assert.Equal(0, stockItem.ReservedQuantity);
            Assert.Contains(stockItem.Movements, mv=> mv.ReferenceId == dbOrder.OrderNumber && mv.Reason == StockMovementReason.Restore);
        });
    }
}