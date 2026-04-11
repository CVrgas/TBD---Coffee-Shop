using System.Net;
using System.Net.Http.Json;
using Application.Common;
using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces.Payment;
using Application.Common.Interfaces.Security;
using Application.Orders.Commands.CreateOrder;
using Application.Orders.Dtos;
using Application.Payments.Commands.ConfirmPayment;
using Application.Payments.Commands.CreatePaymentIntent;
using Domain.Base;
using Domain.Catalog;
using Domain.Inventory;
using Domain.User;
using Infrastructure.Persistence;
using Integration.Common;
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
            var hasher = services.GetRequiredService<IPasswordManager>();
            var uniqueEmail = $"user{Guid.NewGuid()}@mail.com";
            
            // user
            var user = userRole == UserRole.Customer
                ? User.CreateCustomer(
                    email: uniqueEmail,
                    firstName: "user",
                    lastName: "generic",
                    passwordHash: hasher.HashPassword("password123!")
                )
                : User.CreateStaff(
                    email: uniqueEmail,
                    firstName: "user",
                    lastName: "generic",
                    role: userRole,
                    passwordHash: hasher.HashPassword("password123!")
                );
            
            context.Users.Add(user);
            await context.SaveChangesAsync();
            
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
    public async Task CreateOrder_ConcurrentRequests_ShouldReturnConsistentResponsesAndSingleEntry()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();
        const int totalRequest = 10;
        
        var seed = await Seed();
        SetUserContext(seed.UserId, UserRole.Customer);
        
        using var barrier = new Barrier(totalRequest);
        var tasks = new List<Task<HttpResponseMessage>>();

        var request = new CreateOrderCommand("USD", new List<OrderItemDto> { new(seed.ProductId, 1) });
        
        for (var i = 0; i < totalRequest; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                barrier.SignalAndWait();
                return await PostWithIdempotencyAsync("/api/v1/orders", request, idempotencyKey);
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
        
        var orderRequest = new CreateOrderCommand("USD", new List<OrderItemDto> { new(seed.ProductId, 1) });
        var orderResponse = await PostWithIdempotencyAsync("/api/v1/orders", orderRequest, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.OK, orderResponse.StatusCode);
        
        var result = await orderResponse.Content.ReadFromJsonAsync<Envelope<string>>();
        Assert.NotNull(result?.Data);
        var orderNumber = result!.Data;
        
        var createPaymentRequest = new CreatePaymentIntentCommand(orderNumber!);
        var createPaymentResponse = await PostWithIdempotencyAsync("api/v1/payments/intents", createPaymentRequest, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.OK, createPaymentResponse.StatusCode);
        
        var createPayment = await createPaymentResponse.Content.ReadFromJsonAsync<Envelope<PaymentConfirmationResult>>();
        Assert.NotNull(createPayment?.Data);
        
        using var barrier = new Barrier(totalRequest);
        var tasks = new List<Task<HttpResponseMessage>>();

        var idempotencyKey = Guid.NewGuid().ToString();
        for (var i = 0; i < totalRequest; i++)
        {
            var request = new ConfirmPaymentCommand(createPayment.Data.IntentId);
            tasks.Add(Task.Run(async () =>
            {
                barrier.SignalAndWait();
                return await PostWithIdempotencyAsync("api/v1/payments/confirm", request, idempotencyKey);
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