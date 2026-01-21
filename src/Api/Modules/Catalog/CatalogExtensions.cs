using Application.Catalog.Dtos;
using Application.Catalog.Interfaces;
using Application.Catalog.Services;
using Application.Catalog.Validators;
using FluentValidation;
using Infrastructure.Persistence.Abstractions;
using Infrastructure.Persistence.Queries;

namespace Api.Modules.Catalog;

/// <summary>
/// Extension methods for setting up catalog services in the dependency injection container.
/// </summary>
public static class CatalogExtensions
{
    /// <summary>
    /// Adds catalog-related services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection with catalog services added.</returns>
    public static IServiceCollection AddCatalog(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductQueryService, ProductQueryService>();
        services.AddScoped<IValidator<ProductCreateDto>, ProductValidator>();
        services.AddScoped<IValidator<List<ProductCreateDto>>, BulkProductValidator>();
        services.AddScoped<IValidator<ProductUpdateDto>, ProductUpdateValidator>();
        
        services.AddScoped<IProductCategoryService, ProductCategoryService>();
        services.AddScoped<ICategoryQueryService, CategoryQueryService>();
        services.AddScoped<IValidator<ProductCategoryCreateDto>, ProductCategoryValidator>();
        
        return services;
    }
}