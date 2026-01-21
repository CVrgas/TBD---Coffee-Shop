using Application.Catalog.Dtos;
using Application.Catalog.Interfaces;
using Application.Catalog.Services;
using Application.Catalog.Validators;
using FluentValidation;

namespace Api.Modules.Catalog;

/// <summary>
/// Extension methods for setting up product category services in the dependency injection container.
/// </summary>
public static class ProductCategoryExtensions2
{
    /// <summary>
    /// Adds product category-related services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection with product category services added.</returns>
    public static IServiceCollection AddProductCategory(this IServiceCollection services)
    {
        services.AddScoped<IProductCategoryService, ProductCategoryService>();
        services.AddScoped<IValidator<ProductCategoryCreateDto>, ProductCategoryValidator>();
        
        return services;
    }
}