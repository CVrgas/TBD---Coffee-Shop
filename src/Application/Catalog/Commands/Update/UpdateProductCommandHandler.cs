using Application.Catalog.Dtos;
using Application.Catalog.Mapping;
using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Commands.Update;

public class UpdateProductCommandHandler(IAppDbContext context) : IRequestHandler<UpdateProductCommand, Envelope<ProductDto>>
{
    public async Task<Envelope<ProductDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        return await context.ExecuteInTransactionAsync(async (_) =>
        {
            var product = await context.Products.FirstOrDefaultAsync(p => p.IsActive && p.Id == request.ProductId, cancellationToken: cancellationToken);
            if (product is null) return Envelope<ProductDto>.NotFound("Product not found.");
            
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var exist = await context.Products.AnyAsync(p => p.Name == request.Name && p.Id != product.Id, cancellationToken: cancellationToken);
                if(exist) return Envelope<ProductDto>.BadRequest().WithError(nameof(product.Name),"Name already exists.");
            }
            
            if (request.CategoryId.HasValue)
            {
                var categoryExist = await context.ProductCategories
                    .AnyAsync(c => c.Id == request.CategoryId.Value, cancellationToken: cancellationToken);
                if(!categoryExist) return Envelope<ProductDto>.NotFound("Category does not exist.");
            }
            
            product.UpdateDetails(request.Name, request.Description, request.CategoryId);
            
            return Envelope<ProductDto>.Ok(product.ToDto());
        }, ct: cancellationToken);
    }
}