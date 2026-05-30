using Application.Catalog.Dtos;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Paginated;
using Application.Common.Extensions;
using Application.Common.Interfaces;
using Application.Inventory.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Queries.GetProductsPaginated;

public class GetProductsPaginatedQueryHandler(IAppDbContext context, IInventoryQueries inventoryQueries) : IRequestHandler<GetProductsPaginatedQuery, Envelope<Paginated<ProductDto>>>
{
    public async Task<Envelope<Paginated<ProductDto>>> Handle(GetProductsPaginatedQuery request, CancellationToken cancellationToken)
    {
        var queryable = context.Products.AsNoTracking()
            .Where(c =>
                (!request.Request.OnlyActive.HasValue || request.Request.OnlyActive.Value == c.IsActive)  &&
                (string.IsNullOrWhiteSpace(request.Request.QueryPattern) || c.Name.Contains(request.Request.QueryPattern)))
            .ApplySort(request.Request.SortOption);
        
        var totalCount = await queryable.CountAsync(cancellationToken);
        
        queryable = queryable.Skip(request.Request.Skip).Take(request.Request.PageSize!.Value);
        
        var products = await queryable.Select(ProductMappingExtensions.ProductDtoProjection).ToListAsync(cancellationToken);
        var productsIds = products.Select(p => p.Id).ToHashSet();
        
        var stocks = await inventoryQueries.GetAvailableStock(productsIds, cancellationToken);

        foreach (var product in products)
        {
            if(stocks.TryGetValue(product.Id, out var stock)) product.SetQuantityInStock(stock);
        }

        return Envelope<Paginated<ProductDto>>.Ok(new Paginated<ProductDto>(products, totalCount, request.Request.PageIndex!.Value, request.Request.PageSize.Value));
    }
}
