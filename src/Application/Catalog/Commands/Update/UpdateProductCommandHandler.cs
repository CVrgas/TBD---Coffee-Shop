using Application.Auth.Specifications;
using Application.Catalog.Dtos;
using Application.Catalog.Mapping;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Domain.Catalog;
using MediatR;

namespace Application.Catalog.Commands.Update;

public class UpdateProductCommandHandler(
    IRepository<Product, int> repository, 
    IRepository<ProductCategory, int> categoryRepo,
    IUnitOfWork uOw) : IRequestHandler<UpdateProductCommand, Envelope<ProductDto>>
{
    public async Task<Envelope<ProductDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        return await uOw.ExecuteInTransactionAsync(async (_) =>
        {
            var product = await repository.GetByIdAsync(request.ProductId, asNoTracking: false, ct: cancellationToken);
            if (product is null) return Envelope<ProductDto>.NotFound("Product not found.");
            
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var exist = await repository.ExistsAsync(new ProductNameSpec(request.Name, request.ProductId), ct: cancellationToken);
                if(exist) return Envelope<ProductDto>.BadRequest().WithError(nameof(product.Name),"Name already exists.");
            }
            
            if (request.CategoryId.HasValue)
            {
                var categoryExist = await categoryRepo.ExistsAsync(new CategoriesByIdsSpec([request.CategoryId.Value]), ct: cancellationToken);
                if(!categoryExist) return Envelope<ProductDto>.NotFound("Category does not exist.");
            }
            
            product.UpdateDetails(request.Name, request.Description, request.CategoryId);
            
            return Envelope<ProductDto>.Ok(product.ToDto());
        }, ct: cancellationToken);
    }
}