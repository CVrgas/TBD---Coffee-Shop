using System.Net;
using System.Net.Http.Json;
using Application.Common;
using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces.Security;
using Application.Inventory.Commands.AdjustStock;
using Application.Inventory.Dtos;
using Domain.Base.Enum;
using Domain.Catalog;
using Domain.Inventory;
using Domain.Users.Entities;
using Domain.Users.Enum;
using Infrastructure.Persistence;
using Integration.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Inventory;

public class InventoryTests(IntegrationTestFactory factory) : BaseIntegrationTest(factory), IAsyncLifetime
{
    private readonly IntegrationTestFactory _factory = factory;
    
    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }
    
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(int ProductId, int UserId)> SeedAsync()
    {
        return await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var passwordManager = services.GetRequiredService<IPasswordManager>();
            
            var user = User.CreateCustomer("staff", "user", "staff@mail.com", passwordManager.HashPassword("password123!"));
            context.Users.Add(user);
            
            var category = ProductCategory.Create("Drinks", "drinks", "DRK", "desc");
            context.ProductCategories.Add(category);
            await context.SaveChangesAsync();

            var product = Product.Create("Latte", Utilities.GenerateSku("DRK"), 4.5m, "USD", category.Id, "desc");
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var stockItem = StockItem.Initialize(product.Id, 1);
            stockItem.AdjustStock(10, StockMovementReason.Unspecified);
            context.StockItems.Add(stockItem);
            await context.SaveChangesAsync();

            return (product.Id, user.Id);
        });
    }

    #region Get

    [Fact]
    public async Task GetStockLevel_ExistingProduct_ReturnsCorrectStockLevels()
    {
        var (productId, userId) = await SeedAsync();
        SetUserContext(userId, UserRole.Admin);
    
        var response = await Client.GetAsync($"/api/v1/inventories/{productId}");
        Assert.True(response.IsSuccessStatusCode, $"Expected success status code but got {(int)response.StatusCode} ({response.StatusCode}).");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<StockLevelDto>>();

        Assert.NotNull(envelope);
        Assert.True(envelope.IsSuccess);
        Assert.NotNull(envelope.Data);
        Assert.Equal(productId, envelope.Data.ProductId);
        Assert.Equal(10, envelope.Data.QuantityInStock);
        Assert.Equal(0, envelope.Data.ReservedQuantity);
        Assert.Equal(10, envelope.Data.AvailableQuantity);
        Assert.Single(envelope.Data.LocationStockLevels);
        Assert.Equal(1, envelope.Data.LocationStockLevels.First().LocationId);
        Assert.Equal(10, envelope.Data.LocationStockLevels.First().AvailableQuantity);
    }

    [Fact]
    public async Task GetStockLevel_MultipleLocationsAndReservations_AggregatesCorrectly()
    {
        var (productId, userId) = await SeedAsync();
        SetUserContext(userId, UserRole.Admin);

        await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
        
            var stock1 = await context.StockItems.FirstAsync(s => s.ProductId == productId && s.LocationId == 1);
            stock1.ReserveStock(3, "ORDER-1");

            var stock2 = StockItem.Initialize(productId, 2);
            stock2.AdjustStock(5, StockMovementReason.Unspecified);
            stock2.ReserveStock(1, "ORDER-2");
            context.StockItems.Add(stock2);
        
            await context.SaveChangesAsync();
        });

        var response = await Client.GetAsync($"/api/v1/inventories/{productId}");
        Assert.True(response.IsSuccessStatusCode, $"Expected success status code but got {(int)response.StatusCode} ({response.StatusCode}).");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<StockLevelDto>>();

        Assert.NotNull(envelope);
        Assert.True(envelope.IsSuccess);
    
        var payload = envelope.Data;
        Assert.NotNull(payload);
    
        Assert.Equal(productId, payload.ProductId);
    
        Assert.Equal(15, payload.QuantityInStock);
        Assert.Equal(4, payload.ReservedQuantity);
        Assert.Equal(11, payload.AvailableQuantity);
    
        Assert.Equal(2, payload.LocationStockLevels.Count);
    
        var loc1 = payload.LocationStockLevels.First(l => l.LocationId == 1);
        Assert.Equal(7, loc1.AvailableQuantity); 
    
        var loc2 = payload.LocationStockLevels.First(l => l.LocationId == 2);
        Assert.Equal(4, loc2.AvailableQuantity); 
    }

    [Fact]
    public async Task GetStockLevel_ProductWithNoStock_ReturnsZeroedLevel()
    {
        var (_, userId) = await SeedAsync();
        SetUserContext(userId, UserRole.Admin);
        var nonExistentProductId = 99999;

        var response = await Client.GetAsync($"/api/v1/inventories/{nonExistentProductId}");
        Assert.True(response.IsSuccessStatusCode, $"Expected success status code but got {(int)response.StatusCode} ({response.StatusCode}).");
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<StockLevelDto>>();

        Assert.NotNull(envelope);
        Assert.True(envelope.IsSuccess);
        Assert.NotNull(envelope.Data);
        Assert.Equal(nonExistentProductId, envelope.Data.ProductId);
        Assert.Equal(0, envelope.Data.QuantityInStock);
        Assert.Equal(0, envelope.Data.ReservedQuantity);
        Assert.Equal(0, envelope.Data.AvailableQuantity);
        Assert.Empty(envelope.Data.LocationStockLevels);
    }

    #endregion

    #region Post

    [Fact]
    public async Task AdjustStock_ValidRequest_ReturnsOkAndUpdatesQuantity()
    {
        // Arrange
        var (productId, userId) = await SeedAsync();
        SetUserContext(userId, UserRole.Admin);

        var command = new AdjustStockCommand(
            ProductId: productId,
            Delta: 5,
            Reason: StockMovementReason.Adjustment,
            ReferenceId: "REF-123"
        );

        // Act
        var response = await PostWithIdempotencyAsync($"/api/v1/inventories/adjust", command, Guid.NewGuid().ToString());

        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Expected success status code but got {(int)response.StatusCode} ({response.StatusCode}).");

        await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var stock = await context.StockItems.FirstOrDefaultAsync(s => s.ProductId == productId);
            Assert.NotNull(stock);
            Assert.Equal(15, stock.QuantityOnHand);
        });
    }
    
    [Fact]
    public async Task AdjustStock_ValidNegativeDelta_ReturnsOkAndReducesQuantity()
    {
        var (productId, userId) = await SeedAsync();
        SetUserContext(userId, UserRole.Admin);

        var command = new AdjustStockCommand(
            ProductId: productId,
            Delta: -5,
            Reason: StockMovementReason.Adjustment,
            ReferenceId: "REF-124"
        );

        var response = await PostWithIdempotencyAsync($"/api/v1/inventories/adjust", command, Guid.NewGuid().ToString());

        Assert.True(response.IsSuccessStatusCode);

        await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var stock = await context.StockItems.FirstOrDefaultAsync(s => s.ProductId == productId);
            Assert.NotNull(stock);
            Assert.Equal(5, stock.QuantityOnHand); 
        });
    }

    [Fact]
    public async Task AdjustStock_InvalidProductId_ReturnsNotFound()
    {
        var (_, userId) = await SeedAsync();
        SetUserContext(userId, UserRole.Admin);

        var command = new AdjustStockCommand(
            ProductId: 99999, 
            Delta: 5,
            Reason: StockMovementReason.Adjustment,
            ReferenceId: "REF-125"
        );

        var response = await PostWithIdempotencyAsync($"/api/v1/inventories/adjust", command, Guid.NewGuid().ToString());

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdjustStock_DeltaLeadsToNegativeStock_ReturnsErrorAndDoesNotUpdate()
    {
        var (productId, userId) = await SeedAsync();
        SetUserContext(userId, UserRole.Admin);

        var command = new AdjustStockCommand(
            ProductId: productId,
            Delta: -15, 
            Reason: StockMovementReason.Adjustment,
            ReferenceId: "REF-126"
        );

        var response = await PostWithIdempotencyAsync($"/api/v1/inventories/adjust", command, Guid.NewGuid().ToString());

        Assert.False(response.IsSuccessStatusCode);

        await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var stock = await context.StockItems.FirstOrDefaultAsync(s => s.ProductId == productId);
            Assert.NotNull(stock);
            Assert.Equal(10, stock.QuantityOnHand); 
        });
    }

    [Fact]
    public async Task AdjustStock_ZeroDelta_ReturnsErrorAndDoesNotUpdate()
    {
        var (productId, userId) = await SeedAsync();
        SetUserContext(userId, UserRole.Admin);

        var command = new AdjustStockCommand(
            ProductId: productId,
            Delta: 0,
            Reason: StockMovementReason.Adjustment,
            ReferenceId: "REF-127"
        );

        var response = await PostWithIdempotencyAsync($"/api/v1/inventories/adjust", command, Guid.NewGuid().ToString());

        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task AdjustStock_ValidRequest_RecordsStockMovement()
    {
        var (productId, userId) = await SeedAsync();
        SetUserContext(userId, UserRole.Admin);
        var referenceId = "REF-128";

        var command = new AdjustStockCommand(
            ProductId: productId,
            Delta: 5,
            Reason: StockMovementReason.Adjustment,
            ReferenceId: referenceId
        );

        await PostWithIdempotencyAsync($"/api/v1/inventories/adjust", command, Guid.NewGuid().ToString());

        await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var stock = await context.StockItems
                .Include(s => s.Movements)
                .FirstOrDefaultAsync(s => s.ProductId == productId);
        
            Assert.NotNull(stock);
            var movement = stock.Movements.OrderByDescending(m => m.CreatedAt).First();
        
            Assert.Equal(5, movement.QuantityDelta);
            Assert.Equal(StockMovementReason.Adjustment, movement.Reason);
            Assert.Equal(referenceId, movement.ReferenceId);
        });
    }

    [Fact]
    public async Task AdjustStock_IdempotentRequest_ProcessesOnlyOnce()
    {
        var (productId, userId) = await SeedAsync();
        SetUserContext(userId, UserRole.Admin);
        var idempotencyKey = Guid.NewGuid().ToString();

        var command = new AdjustStockCommand(
            ProductId: productId,
            Delta: 5,
            Reason: StockMovementReason.Adjustment,
            ReferenceId: "REF-129"
        );

        var response1 = await PostWithIdempotencyAsync($"/api/v1/inventories/adjust", command, idempotencyKey);
        var response2 = await PostWithIdempotencyAsync($"/api/v1/inventories/adjust", command, idempotencyKey);

        Assert.True(response1.IsSuccessStatusCode);
        Assert.True(response2.IsSuccessStatusCode);

        await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var stock = await context.StockItems.FirstOrDefaultAsync(s => s.ProductId == productId);
            Assert.NotNull(stock);
            Assert.Equal(15, stock.QuantityOnHand); 
        });
    }

    #endregion
}