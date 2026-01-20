using Application.Inventory.Services;
using Application.Orders.Dtos;
using Application.Orders.Services;
using Application.Orders.Validators;
using FluentValidation;

namespace Api.Modules.Order;

/// <summary>
/// Extension methods for setting up order services in the dependency injection container.
/// </summary>
public static class OrderExtensions
{
    /// <summary>
    /// Adds order-related services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection with order services added.</returns>
    public static IServiceCollection AddOrder(this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
        
        services.AddScoped<IValidator<OrderCreationDto>,  OrderCreationValidator>();
        services.AddScoped<IValidator<OrderItemDto>,  OrderItemValidator>();
        
        return services;
    }
}