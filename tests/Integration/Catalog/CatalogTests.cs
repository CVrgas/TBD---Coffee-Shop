using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Catalog.Dtos;
using Application.Common;
using Application.Common.Abstractions.Envelope;
using Domain.Base;
using Domain.Catalog;
using Domain.Inventory;
using Domain.User;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Integration.Common;
using Microsoft.AspNetCore.Identity;
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
            var context = services.GetRequiredService<MyDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher<User>>();
            var uniqueEmail = $"user{Guid.NewGuid()}@mail.com";
            
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
            
            var category = new ProductCategory
            {
                Name = "Coffee",
                Slug = ("coffee-" + Guid.NewGuid()).Slugify(),
                Code = "Coffee".ToUpperInvariant().Replace(" ", ""),
                Description = "whole coffee",
            };

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
        var productRequest = new ProductCreateDto
        {
            Name = uniqueName,
            Description = "Premium Arabica Blend",
            Price = 12.50m,
            Currency = "USD",
            CategoryId = seed.CategoryId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/products", productRequest);
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
        Assert.Equal(HttpStatusCode.OK, result?.StatusCode);

        await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<MyDbContext>();
            var createdProduct =
                await context.Products.FirstOrDefaultAsync(p => result != null && p.Id == result.Data.Id);

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
        var productRequest = new ProductCreateDto
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
            var errorsJson = (System.Text.Json.JsonElement)errorsObj!;
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
            var context = services.GetRequiredService<MyDbContext>();
            
            var product = new Product
            {
                Name = uniqueName,
                Sku = $"{Utilities.GenerateSku(seed.CategoryCode)}",
                Price = 5.99m,
                Currency = new CurrencyCode("USD"),
                Description = "best colombian coffee",
                CategoryId = seed.CategoryId,
                StockItems = new List<StockItem>{new() { IsActive = true}},
            };
            
            context.Products.Add(product);
            await context.SaveChangesAsync();
            return product;
        });
        
        // Act
        var firstResponse = await Client.GetAsync($"/api/v1/products/{product.Id}");
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<Envelope<ProductDto>>();
        Assert.Equal(uniqueName, firstResult?.Data?.Name);

        await ExecuteInScopeAsync(async services =>
        {
            var context = services.GetRequiredService<MyDbContext>();
            var p = await context.Products.FindAsync(product.Id);
            p!.Name = "UPDATED_NAME_IN_DB";
            await context.SaveChangesAsync();
        });
        
        // Act
        var secondResponse = await Client.GetAsync($"/api/v1/products/{product.Id}");
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<Envelope<ProductDto>>();
        
        // Assert
        Assert.Equal(firstResult?.Data?.Name, secondResult?.Data?.Name);
        Assert.NotEqual("UPDATED_NAME_IN_DB", secondResult?.Data?.Name);
        
        // Arrange
        var currentVersion = await ExecuteInScopeAsync(async sp =>
        {
            var context = sp.GetRequiredService<MyDbContext>();
            return await context.Products
                .AsNoTracking()
                .Where(p => p.Id == product.Id)
                .Select(p => p.RowVersion)
                .FirstOrDefaultAsync();
        });
        
        // Act
        const decimal newPrice = 99.99m;
        var updateRequest = new ProductUpdatePrice(product.Id, newPrice, "USD", currentVersion);
        var updateResponse = await Client.PatchAsJsonAsync($"/api/v1/products/{product.Id}/price", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        
        // Act
        var finalGet = await Client.GetAsync($"/api/v1/products/{product.Id}");
        var finalResult = await finalGet.Content.ReadFromJsonAsync<Envelope<ProductDto>>();
        
        Assert.NotNull(finalResult?.Data);
        Assert.Equal(newPrice, finalResult.Data.Price);
        Assert.Equal("UPDATED_NAME_IN_DB", finalResult.Data.Name); 
    }
}