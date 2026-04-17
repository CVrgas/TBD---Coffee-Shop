using Application.Catalog.Commands.Create;
using Application.Catalog.Commands.CreateCategory;
using Application.Catalog.Commands.Update;
using Application.Catalog.Dtos;
using Application.Catalog.Interfaces;
using Application.Catalog.Validators;
using FluentValidation;
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
        services.AddScoped<IProductQueryService, ProductQueryService>();
        services.AddScoped<IValidator<CreateProductCommand>, ProductValidator>();
        services.AddScoped<IValidator<List<CreateProductCommand>>, BulkProductValidator>();
        services.AddScoped<IValidator<UpdateProductCommand>, ProductUpdateValidator>();
        
        services.AddScoped<ICategoryQueryService, CategoryQueryService>();
        services.AddScoped<IValidator<CreateCategoryCommand>, ProductCategoryValidator>();
        
        return services;
    }
}