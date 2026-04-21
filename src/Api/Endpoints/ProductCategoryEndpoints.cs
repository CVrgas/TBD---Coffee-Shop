using Api.Middlewares;
using Application.Catalog.Commands.CreateCategory;
using Application.Catalog.Queries.GetCategoriesPaginated;
using Application.Catalog.Queries.GetCategoryById;
using Application.Catalog.Queries.GetCategoryBySlug;
using Application.Common.Abstractions.Persistence.Paginated;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

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
        var group = endpoints.MapGroup("/products/categories")
            .RequireAuthorization()
            .WithTags("ProductCategory");

        group.MapGet("/", async ([AsParameters] PaginatedRequest req, [FromServices] ISender sender, CancellationToken cancellationToken = default) =>
            await sender.Send(new GetCategoriesPaginatedQuery(req), cancellationToken))
            .AddEndpointFilter(new ValidationFilter<PaginatedRequest>());

        group.MapPost("/", async ([FromBody] CreateCategoryCommand req, [FromServices] ISender sender, CancellationToken cancellationToken = default) => 
            await sender.Send(req, cancellationToken))
            .RequireAuthorization()
            .AddEndpointFilter(new ValidationFilter<CreateCategoryCommand>());
        
        group.MapGet("/{id:int}", async (int id, [FromServices] ISender sender, CancellationToken cancellationToken = default) =>
        {
            return await sender.Send(new GetCategoryByIdQuery(id), cancellationToken);
        });
        
        group.MapGet("/{slug}", async (string slug, [FromServices] ISender sender, CancellationToken cancellationToken = default) =>
        {
            return await sender.Send(new GetCategoryBySlugQuery(slug), cancellationToken);
        });
        
        return endpoints;
    }
}