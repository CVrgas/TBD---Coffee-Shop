using Application.Catalog.Dtos;
using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces;
using Application.Inventory.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Queries.GetProductById;

public class GetProductByIdQueryHandler(IAppDbContext context, IInventoryQueries inventoryQueries) : IRequestHandler<GetProductByIdQuery, Envelope<ProductDto>>
{
    public async Task<Envelope<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await context.Products.AsNoTracking()
            .Where(p => p.Id == request.Id)
            .Select(ProductMappingExtensions.ProductDtoProjection)
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null) return Envelope<ProductDto>.NotFound();

        var stockQuantity = await inventoryQueries.GetAvailableStock(product.Id, cancellationToken);
        product.SetQuantityInStock(stockQuantity);

        return Envelope<ProductDto>.Ok(product);
    }
}
