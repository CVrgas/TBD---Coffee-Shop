using Application.Catalog.Dtos;
using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces;
using Application.Inventory.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Queries.GetProductBySku;

public class GetProductBySkuQueryHandler(IAppDbContext context, IInventoryQueries inventoryQueries) : IRequestHandler<GetProductBySkuQuery, Envelope<ProductDto>>
{
    public async Task<Envelope<ProductDto>> Handle(GetProductBySkuQuery request, CancellationToken cancellationToken)
    {
        var product = await context.Products.AsNoTracking()
            .Where(p => p.Sku == request.Sku)
            .Select(ProductMappingExtensions.ProductDtoProjection)
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null) return Envelope<ProductDto>.NotFound();

        var stock = await inventoryQueries.GetAvailableStock(product.Id, cancellationToken);
        product.SetQuantityInStock(stock);

        return Envelope<ProductDto>.Ok(product);
    }
}
