using Application.Inventory.Abstractions;
using Application.Inventory.Dtos;
using Application.Inventory.Services;
using Application.Inventory.Validators;
using FluentValidation;
using Infrastructure.Persistence.Abstractions;

namespace Api.Modules.Inventory;

/// <summary>
/// Extension methods for setting up inventory services in the dependency injection container.
/// </summary>
public static class InventoryExtensions
{
    /// <summary>
    /// Adds inventory-related services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection with inventory services added.</returns>
    public static IServiceCollection AddInventory(this IServiceCollection services)
    {
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IValidator<AdjustStockDto>, StockValidator>();
        return services;
    }
}