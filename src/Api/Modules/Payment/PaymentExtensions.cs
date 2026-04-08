using Application.Common.Interfaces.Payment;
using Infrastructure.Payment;

namespace Api.Modules.Payment;

/// <summary>
/// Extension methods for setting up payment services in the dependency injection container.
/// </summary>
public static class PaymentExtensions
{
    /// <summary>
    /// Adds payment-related services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection with payment services added.</returns>
    public static IServiceCollection AddPayment(this IServiceCollection services)
    {
        services.AddScoped<IPaymentGateway, MockPaymentGateway>();
        
        return services;
    }
}