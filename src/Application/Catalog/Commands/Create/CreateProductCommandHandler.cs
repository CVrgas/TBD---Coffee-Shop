using Application.Catalog.Dtos;
using Application.Catalog.Mapping;
using Application.Common;
using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces;
using Domain.Catalog;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Commands.Create;

public class CreateProductCommandHandler(IAppDbContext context) : IRequestHandler<CreateProductCommand, Envelope<ProductDto>>
{
    public async Task<Envelope<ProductDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        return await context.ExecuteInTransactionAsync(async (_) =>
        {
            var exist = await context.Products.AnyAsync(p => p.Name == request.Name, cancellationToken: cancellationToken);
            if (exist)
                return Envelope<ProductDto>.BadRequest()
                    .WithError(nameof(request.Name), "Product name already exists");
            
            var category = await context.ProductCategories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken: cancellationToken);

            if (category is null)
                return Envelope<ProductDto>.BadRequest("Category does not exist")
                    .WithError(nameof(request.CategoryId), "Category does not exist");

            var entity = Product.Create(
                name: request.Name,
                sku: $"{Utilities.GenerateSku(category.Code)}",
                price: request.Price,
                currencyCode: request.Currency,
                description: request.Description,
                imageUrl: request.ImageUrl,
                categoryId: request.CategoryId
            );

            await context.Products.AddAsync(entity, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Envelope<ProductDto>.Ok(entity.ToDto() with { CategoryName = category.Name });
        }, cancellationToken);
    }
}