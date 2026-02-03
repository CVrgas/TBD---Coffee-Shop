using Application.Common;
using Application.Common.Interfaces;
using Domain.Base;
using Domain.Catalog;
using Domain.Inventory;
using Domain.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Seeding;

public class DataSeeder(ApplicationDbContext context, IPasswordHasher<User> passwordHasher) : IDataSeeder
{
    private readonly List<ProductCategory> _categories =
    [
        new()
        {
            Name = "Coffee",
            Code = "COF-001",
            Slug = "coffee".Slugify(), 
            Description = "Premium beans sourced ethically.",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        },
        new()
        {
            Name = "Pastries",
            Code = "PAS-001",
            Slug = "pastries".Slugify(),
            Description = "Freshly baked goods.",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        }
    ];

    public async Task SeedAsync()
    {
        if (!await context.ProductCategories.AnyAsync())
        {
            await context.ProductCategories.AddRangeAsync(_categories);
            await context.SaveChangesAsync();
        }
        
        if (!await context.Products.AnyAsync())
        {
            await SeedProductsAsync();
        }
        
        if (!await context.Users.AnyAsync())
        {
            await SeedUsersAsync();
        }
    }

    private StockItem GenerateStockItem(int quantity, bool isActive)
    {
        var stockItem = new StockItem{ RowVersion = [], LocationId = 1, IsActive = isActive, LastMovementAt = DateTime.UtcNow, };
        stockItem.AdjustQuantity(quantity);
        return stockItem;
    }
    private async Task SeedProductsAsync()
    {
        var coffeeCategory = await context.ProductCategories.FirstAsync(c => c.Code == "COF-001");
        var pastryCategory = await context.ProductCategories.FirstAsync(c => c.Code == "PAS-001");

        var products = new List<Product>
        {
            new()
            {
                Name = "Espresso",
                Sku = "COF-ESP-01",
                Price = 2.50m, // Cheap
                Currency = new CurrencyCode("USD"),
                Description = "Strong, dark, and essential.",
                ImageUrl = "https://placehold.co/400x300?text=Espresso",
                IsActive = true,
                CategoryId = coffeeCategory.Id,
                CreatedAt = DateTime.UtcNow,
                StockItems = new List<StockItem> { GenerateStockItem(20, true) },
            },
            new()
            {
                Name = "Caramel Latte",
                Sku = "COF-LAT-02",
                Price = 5.50m, // Standard
                Currency = new CurrencyCode("USD"),
                Description = "Steamed milk with a shot of espresso and caramel syrup.",
                ImageUrl = "https://placehold.co/400x300?text=Latte",
                IsActive = true,
                CategoryId = coffeeCategory.Id,
                CreatedAt = DateTime.UtcNow,
                StockItems = new List<StockItem> { GenerateStockItem(10, true) },
            },
            new()
            {
                Name = "Premium Origin Bundle",
                Sku = "COF-BUN-99",
                Price = 50.00m, // Expensive 
                Currency = new CurrencyCode("USD"),
                Description = "A selection of our finest beans.",
                ImageUrl = "https://placehold.co/400x300?text=Bundle",
                IsActive = true,
                CategoryId = coffeeCategory.Id,
                CreatedAt = DateTime.UtcNow,
                StockItems = new List<StockItem> { GenerateStockItem(5, true) },
            },
            new()
            {
                Name = "Butter Croissant",
                Sku = "PAS-CRO-01",
                Price = 3.00m,
                Currency = new CurrencyCode("USD"),
                Description = "Flaky and buttery.",
                ImageUrl = "https://placehold.co/400x300?text=Croissant",
                IsActive = true,
                CategoryId = pastryCategory.Id,
                CreatedAt = DateTime.UtcNow,
                StockItems = new List<StockItem> { GenerateStockItem(25, true) },
            },
            new()
            {
                Name = "Stale Cookie",
                Sku = "PAS-OLD-00",
                Price = 0.50m,
                Currency = new CurrencyCode("USD"),
                Description = "You should not see this in the catalog.",
                ImageUrl = "https://placehold.co/400x300?text=No",
                IsActive = false,
                CategoryId = pastryCategory.Id,
                CreatedAt = DateTime.UtcNow,
                StockItems = new List<StockItem> { GenerateStockItem(50, true) },
            }
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }

    private async Task SeedUsersAsync()
    {
        var admin = new User
        {
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@commerce.com",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        // Hash password: "Password123!"
        admin.PasswordHash = passwordHasher.HashPassword(admin, "Password123!");

        var customer = new User
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "customer@commerce.com",
            Role = UserRole.Customer,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        customer.PasswordHash = passwordHasher.HashPassword(customer, "Password123!");

        await context.Users.AddRangeAsync(admin, customer);
        await context.SaveChangesAsync();
    }
}