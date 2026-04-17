using Application.Auth.Specifications;
using Application.Catalog.Dtos;
using Application.Catalog.Mapping;
using Application.Common;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Domain.Catalog;
using MediatR;

namespace Application.Catalog.Commands.Create;

public class CreateProductCommandHandler(
    IRepository<Product, int> repository, 
    IRepository<ProductCategory, int> categoryRepo,
    IUnitOfWork uOw) : IRequestHandler<CreateProductCommand, Envelope<ProductDto>>
{
    public async Task<Envelope<ProductDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        return await uOw.ExecuteInTransactionAsync(async (_) =>
        {
            var exist = await repository.ExistsAsync(new ProductNameSpec(request.Name), ct: cancellationToken);
            if (exist)
                return Envelope<ProductDto>.BadRequest()
                    .WithError(nameof(request.Name), "Product name already exists");

            var category = await categoryRepo.GetByIdAsync(id: request.CategoryId, ct: cancellationToken);

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

            await repository.Create(entity);
            await uOw.SaveChangesAsync(cancellationToken);
            return Envelope<ProductDto>.Ok(entity.ToDto() with { CategoryName = category.Name });
        }, cancellationToken);
    }
}