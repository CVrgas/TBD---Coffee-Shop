using System.Net;
using System.Net.Http.Json;
using Api.Modules.Payment;
using Application.Common;
using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces.Payment;
using Application.Orders.Dtos;
using Domain.Base;
using Domain.Catalog;
using Domain.Inventory;
using Domain.User;
using Infrastructure.Persistence;
using Integration.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Order;

public class OrderIdempotencyTests(IntegrationTestFactory factory) : BaseIntegrationTest(factory), IAsyncLifetime
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
    public async Task CreateOrder_ConcurrentRequests_ShouldReturnConsistentResponsesAndSingleEntry()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();
        const int totalRequest = 10;
        
        var seed = await Seed();
        SetUserContext(seed.UserId, UserRole.Customer);
        
        using var barrier = new Barrier(totalRequest);
        var tasks = new List<Task<HttpResponseMessage>>();

        var request = new OrderCreationDto("USD", new List<OrderItemDto> { new(seed.ProductId, 1) });
        
        for (var i = 0; i < totalRequest; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                barrier.SignalAndWait();
                return await PostWithIdempotencyAsync("api/v1/order/add", request, idempotencyKey);
            }));
        }

        var responses = await Task.WhenAll(tasks);
        
        var successResponses = responses
            .Where(r => r.IsSuccessStatusCode)
            .ToList();
        
        var failedResponses = responses
            .Where(r => r.StatusCode == HttpStatusCode.Conflict)
            .ToList();
        
        // Assert
        Assert.True(successResponses.Count >= 1, "At least one order should be created");

        await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var orderCount = await context.Orders.CountAsync();

            Assert.Equal(1, orderCount);
        });
        
        var firstResponseBody = await successResponses.First().Content.ReadAsStringAsync();
        foreach (var r in successResponses.Skip(1))
        {
            var currentBody = await r.Content.ReadAsStringAsync();
            Assert.Equal(firstResponseBody, currentBody);
        }
    }
    
    [Fact]
    public async Task ConfirmPayment_ConcurrentRequests_ShouldEnsureAtomicity()
    {
        // Arrange
        const int totalRequest = 10;
        var seed = await Seed();
        SetUserContext(seed.UserId, UserRole.Customer);
        
        var orderRequest = new OrderCreationDto("USD", new List<OrderItemDto> { new(seed.ProductId, 1) });
        var orderResponse = await PostWithIdempotencyAsync("api/v1/order/add", orderRequest, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.OK, orderResponse.StatusCode);
        
        var result = await orderResponse.Content.ReadFromJsonAsync<Envelope<string>>();
        Assert.NotNull(result?.Data);
        var orderNumber = result!.Data;
        
        var createPaymentRequest = new PayCreateRequest(orderNumber!);
        var createPaymentResponse = await Client.PostAsJsonAsync("api/v1/payment/create", createPaymentRequest);
        Assert.Equal(HttpStatusCode.OK, createPaymentResponse.StatusCode);
        
        var createPayment = await createPaymentResponse.Content.ReadFromJsonAsync<Envelope<PaymentConfirmationResult>>();
        Assert.NotNull(createPayment?.Data);
        
        using var barrier = new Barrier(totalRequest);
        var tasks = new List<Task<HttpResponseMessage>>();

        var idempotencyKey = Guid.NewGuid().ToString();
        for (var i = 0; i < totalRequest; i++)
        {
            var request = new PayConfirmationRequest(createPayment.Data.IntentId, orderNumber);
            tasks.Add(Task.Run(async () =>
            {
                barrier.SignalAndWait();
                return await PostWithIdempotencyAsync("api/v1/payment/confirm", request, idempotencyKey);
            }));
        }

        var responses = await Task.WhenAll(tasks);
        Assert.Empty(responses.Where(r => r.StatusCode == HttpStatusCode.BadRequest).ToList());

        var successResponses = responses
            .Where(r => r.IsSuccessStatusCode)
            .ToList();
        
        Assert.True(successResponses.Count >= 1, "At least one order should be created");

        await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var records = await context.PaymentRecords
                .Where(pr => pr.IntentId == createPayment!.Data.IntentId)
                .ToListAsync();
            
            Assert.Single(records);
            Assert.Equal(PaymentStatus.Approved, records.First().Status);
        });
        
        var firstResponseBody = await successResponses.First().Content.ReadAsStringAsync();
        foreach (var r in successResponses.Skip(1))
        {
            var currentBody = await r.Content.ReadAsStringAsync();
            Assert.Equal(firstResponseBody, currentBody);
        }
    }
}