using Api.Middlewares;
using Application.Catalog.Dtos;
using Application.Catalog.Services;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Paginated;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Catalog;

/// <summary>
/// Defines the endpoints for product category operations.
/// </summary>
public static class ProductCategoryEndpoints
{
    /// <summary>
    /// Maps the product category endpoints to the application's request pipeline.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder with product category endpoints mapped.</returns>
    public static IEndpointRouteBuilder MapProductCategory(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/product/category")
            .WithTags("ProductCategory");

        group.MapGet("/", async (string? query, SortOption? sort, int? take, IProductCategoryService svc) =>
            await svc.GetAllAsync(query, sort, take));

        group.MapPost("/", async ([FromBody] ProductCategoryCreateDto req, IProductCategoryService svc) =>
            await svc.AddAsync(req))
            .AddEndpointFilter(new ValidationFilter<ProductCategoryCreateDto>());
        
        group.MapGet("/{id:int}", async (int id, IProductCategoryService svc) =>
            await svc.GetByIdAsync(id));
        
        group.MapGet("/{slug}", async (string slug, IProductCategoryService svc) =>
            await svc.GetBySlugAsync(slug));
        
        group.MapGet("/{slug}/products", async ([FromRoute] string slug, [AsParameters] PaginatedRequest req, IProductService pService) =>
            await pService.GetPaginatedByCategoryAsync(slug, req))
            .AddEndpointFilter(new ValidationFilter<PaginatedRequest>());
        
        return endpoints;
    }
}