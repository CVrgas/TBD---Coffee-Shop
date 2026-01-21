using Api.Middlewares;
using Application.Catalog.Dtos;
using Application.Catalog.Interfaces;
using Application.Catalog.Services;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Paginated;
using Infrastructure.Persistence.Abstractions;
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

        group.MapGet("/", async ([AsParameters] PaginatedRequest req, ICategoryQueryService svc) =>
            Envelope<Paginated<ProductCategoryDto>>.Ok(await svc.GetAllAsync(req)))
            .AddEndpointFilter(new ValidationFilter<PaginatedRequest>());

        group.MapPost("/", async ([FromBody] ProductCategoryCreateDto req, IProductCategoryService svc) =>
            await svc.AddAsync(req))
            .AddEndpointFilter(new ValidationFilter<ProductCategoryCreateDto>());
        
        group.MapGet("/{id:int}", async (int id, ICategoryQueryService svc) =>
        {
            var category = await svc.GetByIdAsync(id);
            return category == null ? Envelope<ProductCategoryDto>.NotFound() : Envelope<ProductCategoryDto>.Ok(category);
        });
        
        group.MapGet("/{slug}", async (string slug, ICategoryQueryService svc) =>
        {
            var category = await svc.GetBySlugAsync(slug);
            return category == null ? Envelope<ProductCategoryDto>.NotFound() : Envelope<ProductCategoryDto>.Ok(category);
        });
        
        return endpoints;
    }
}