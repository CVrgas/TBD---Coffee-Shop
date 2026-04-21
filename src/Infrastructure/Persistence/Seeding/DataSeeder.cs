using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Security;
using Domain.Base.Enum;
using Domain.Catalog;
using Domain.Inventory;
using Domain.Users.Entities;
using Domain.Users.Enum;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Seeding;

public class DataSeeder(ApplicationDbContext context, IPasswordManager passwordHasher) : IDataSeeder
{
    private readonly List<ProductCategory> _categories =
    [
        ProductCategory.Create(
            name: "Coffee",
            slug: "coffee".Slugify(),
            code: "COF-001",
            description: "Premium beans sourced ethically."
            ),
        
        ProductCategory.Create(
            name: "Pastries",
            code: "PAS-001",
            slug: "pastries".Slugify(),
            description: "Freshly baked goods."
            )
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

    private StockItem GenerateStockItem(int product, int quantity, int locationId = 1)
    {
        var stockItem = StockItem.Initialize(product, locationId);
        stockItem.ReceiveStock(quantity);
        return stockItem;
    }
    
    private async Task SeedProductsAsync()
    {
        var coffeeCategory = await context.ProductCategories.FirstAsync(c => c.Code == "COF-001");
        var pastryCategory = await context.ProductCategories.FirstAsync(c => c.Code == "PAS-001");

        var products = new List<Product>
        {
            Product.Create(                
                name: "Espresso",
                sku: "COF-ESP-01",
                price: 2.50m, // Cheap
                currencyCode: "USD",
                description: "Strong, dark, and essential.",
                imageUrl: "https://placehold.co/400x300?text=Espresso",
                categoryId: coffeeCategory.Id
                ),
            Product.Create(
                name: "Caramel Latte",
                sku: "COF-LAT-02",
                price: 5.50m, // Standard
                currencyCode: "USD",
                description: "Steamed milk with a shot of espresso and caramel syrup.",
                imageUrl: "https://placehold.co/400x300?text=Latte",
                categoryId: coffeeCategory.Id
                ),
            Product.Create(
                name: "Premium Origin Bundle",
                sku: "COF-BUN-99",
                price: 50.00m, // Expensive 
                currencyCode: "USD",
                description: "A selection of our finest beans.",
                imageUrl: "https://placehold.co/400x300?text=Bundle",
                categoryId: coffeeCategory.Id
                ),
            Product.Create(
                name: "Butter Croissant",
                sku: "PAS-CRO-01",
                price: 3.00m,
                currencyCode: "USD",
                description: "Flaky and buttery.",
                imageUrl: "https://placehold.co/400x300?text=Croissant",
                categoryId: pastryCategory.Id
                ),
            Product.Create(
                name: "Stale Cookie",
                sku: "PAS-OLD-00",
                price: 0.50m,
                currencyCode: "USD",
                description: "You should not see this in the catalog.",
                imageUrl: "https://placehold.co/400x300?text=No",
                categoryId: pastryCategory.Id
                )
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();

        foreach (var product in products)
        {
            var stock = StockItem.Initialize(product.Id);
            stock.AdjustStock(25, StockMovementReason.Unspecified, reference: "seeding");
            await context.StockItems.AddAsync(stock);
        }
        
        await  context.SaveChangesAsync();
    }

    private async Task SeedUsersAsync()
    {
        var admin = User.CreateStaff(
            firstName: "Admin",
            lastName: "User",
            email: "admin@commerce.com",
            role: UserRole.Admin,
            passwordHash: passwordHasher.HashPassword("Password123!")
            );

        var customer = User.CreateCustomer(
            firstName: "John",
            lastName: "Doe",
            email: "customer@commerce.com",
            passwordHash: passwordHasher.HashPassword("Password123!")
            );

        await context.Users.AddRangeAsync(admin, customer);
        await context.SaveChangesAsync();
    }
}