using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Catalog.Commands.Create;
using Application.Catalog.Commands.UpdatePrice;
using Application.Catalog.Dtos;
using Application.Common;
using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces.Security;
using Domain.Base.Enum;
using Domain.Catalog;
using Domain.Inventory;
using Domain.Users.Entities;
using Domain.Users.Enum;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Integration.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Catalog;

public sealed record SeedResult(int CategoryId, int UserId, string CategoryCode);

public class CatalogTests(IntegrationTestFactory factory) : BaseIntegrationTest(factory), IAsyncLifetime
{
    private readonly IntegrationTestFactory _factory = factory;

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<SeedResult> SeedCategoryAsync(UserRole userRole = UserRole.Customer)
    {
        return await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var hasher = services.GetRequiredService<IPasswordManager>();
            var uniqueEmail = $"user{Guid.NewGuid()}@mail.com";
            
            var user = User.CreateCustomer(
                firstName: "user", 
                lastName: "generic",
                email: uniqueEmail,
                passwordHash: hasher.HashPassword("password123!")
                );

            var category = ProductCategory.Create(
                name: "Coffee",
                slug: ("coffee-" + Guid.NewGuid()).Slugify(),
                code: "Coffee".ToUpperInvariant().Replace(" ", ""),
                description: "whole coffee"
            );

            context.Users.Add(user);
            context.ProductCategories.Add(category);
            await context.SaveChangesAsync();
            return new SeedResult(category.Id, user.Id, category.Code);
        });
    }
    
    [Fact]
    public async Task CreateProduct_ValidData_ReturnOkAndPersist()
    {
        // Arrange
        // seed category
        var seed = await SeedCategoryAsync();
                SetUserContext(seed.UserId, UserRole.Admin);
        
        // Create unique name
        var uniqueName = $"Coffee_{Guid.NewGuid()}";
        var productRequest = new CreateProductCommand
        {
            Name = uniqueName,
            Description = "Premium Arabica Blend",
            Price = 12.50m,
            Currency = "USD",
            CategoryId = seed.CategoryId
        };

        // Act
        var response = await PostWithIdempotencyAsync("/api/v1/products", productRequest, Guid.NewGuid().ToString());
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Status: {response.StatusCode}, Body: {errorBody}");
        }
        
        var result = await response.Content.ReadFromJsonAsync<Envelope<ProductDto>>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.IsType<Envelope<ProductDto>>(result);
        Assert.NotNull(result.Data);
        Assert.InRange(result.Data.Id, 0, int.MaxValue);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var createdProduct =
                await context.Products.FirstOrDefaultAsync(p => p.Id == result.Data.Id);

            Assert.NotNull(createdProduct);
            Assert.Equal(productRequest.Description, createdProduct.Description);
            Assert.Equal(seed.CategoryId, createdProduct.CategoryId);
            Assert.True(createdProduct.IsActive);
        });
    }

    [Fact]
    public async Task CreateProduct_InvalidData_FailAndReturnBadRequest()
    {
        // Arrange
        var seed = await SeedCategoryAsync();
        SetUserContext(seed.UserId, UserRole.Admin); // Set user

        var uniqueName = $"Coffee_{Guid.NewGuid()}";
        var productRequest = new CreateProductCommand
        {
            Name = uniqueName,
            Description = "Not so good coffee",
            Price = -20m,
            Currency = "USD",
            CategoryId = seed.CategoryId
        };
        
        // act
        var response = await Client.PostAsJsonAsync("/api/v1/products", productRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        
        // assert
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
        Assert.Equal("Bad Request", problem.Title);
        
        if (problem.Extensions.TryGetValue("errors", out var errorsObj))
        {
            var errorsJson = (JsonElement)errorsObj!;
            var errors = errorsJson.Deserialize<Dictionary<string, string[]>>();
        
            Assert.Contains(errors!, e => e.Key.Contains("Price"));
            Assert.Equal("Price must be greater than 0", errors!["Price"][0]);
        }
        else
        {
            Assert.Fail("El ProblemDetails no contiene la extensión 'errors'.");
        }
    }

    [Fact]
    public async Task CreateProduct_ValidData_ReturnOkAndPersistCache()
    {
        // Arrange
        var seed = await SeedCategoryAsync();
        SetUserContext(seed.UserId, UserRole.Admin);
        var uniqueName = $"Coffee_{Guid.NewGuid()}";
        
        var product = await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            
            var product = Product.Create(
                    name: uniqueName,
                    sku: $"{Utilities.GenerateSku(seed.CategoryCode)}",
                    price: 5.99m,
                    currencyCode: "USD",
                    description: "best colombian coffee",
                    categoryId: seed.CategoryId
                    );
            
            context.Products.Add(product);
            await context.SaveChangesAsync();
            
            var stockItem = StockItem.Initialize(
                productId: product.Id,
                locationId: 1 // Assuming a default location for testing
            );
            
            stockItem.AdjustStock(100, StockMovementReason.Unspecified); // Add initial stock
            context.StockItems.Add(stockItem);
            await context.SaveChangesAsync();
            
            return product;
        });
        
        // Act
        var firstResponse = await Client.GetAsync($"/api/v1/products/{product.Id}");
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<Envelope<ProductDto>>();
        Assert.Equal(uniqueName, firstResult?.Data?.Name);

        await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var p = await context.Products.FindAsync(product.Id);
            p!.SetPrivateProperty("Name", "UPDATED_NAME_IN_DB");
            await context.SaveChangesAsync();
        });
        
        // Act
        var secondResponse = await Client.GetAsync($"/api/v1/products/{product.Id}");
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<Envelope<ProductDto>>();
        
        // Assert
        Assert.Equal(firstResult?.Data?.Name, secondResult?.Data?.Name);
        Assert.NotEqual("UPDATED_NAME_IN_DB", secondResult?.Data?.Name);
        
        // Act
        const decimal newPrice = 99.99m;
        var updateRequest = new UpdatePriceCommand(product.Id, newPrice, "USD");
        var updateResponse = await Client.PatchAsJsonAsync($"/api/v1/products/{product.Id}/price", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        
        // Act
        var finalGet = await Client.GetAsync($"/api/v1/products/{product.Id}");
        var finalResult = await finalGet.Content.ReadFromJsonAsync<Envelope<ProductDto>>();
        
        // Assert
        Assert.NotNull(finalResult?.Data);
        Assert.Equal(newPrice, finalResult.Data.Price);
        Assert.Equal("UPDATED_NAME_IN_DB", finalResult.Data.Name); 
    }
}