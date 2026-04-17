using Application.Catalog.Dtos;
using Application.Catalog.Mapping;
using Application.Catalog.Specifications;
using Application.Common;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Domain.Catalog;
using MediatR;

namespace Application.Catalog.Commands.CreateCategory;

public class CreateCategoryCommandHandler(
    IRepository<ProductCategory, int> repository,
    IUnitOfWork uoW) 
    : IRequestHandler<CreateCategoryCommand,  Envelope<ProductCategoryDto>>
{
    public async Task<Envelope<ProductCategoryDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        return await uoW.ExecuteInTransactionAsync(async _ =>
        {
            var name = request.Name!.Trim();
            var slug = name.Slugify();

            if (request.ParentId.HasValue)
            {
                var parent = await repository.GetByIdAsync(request.ParentId.Value, ct: cancellationToken);
                if (parent is null || !parent.IsActive) 
                    return Envelope<ProductCategoryDto>.BadRequest()
                        .WithError(nameof(request.ParentId), "category not found or inactive.");
            }
            
            var exist = await repository.ExistsAsync(new CategoriesSlugsSpec([slug]), ct: cancellationToken);
            if (exist) 
                return Envelope<ProductCategoryDto>.BadRequest()
                    .WithError(nameof(request.ParentId), "Category already exists.");
            
            if(string.IsNullOrWhiteSpace(request.Code)) return Envelope<ProductCategoryDto>.BadRequest()
                .WithError(nameof(request.Code), "Category Code is required.");

            var newCategory = ProductCategory.Create(
                name,
                slug,
                code: request.Code,
                description: request.Description,
                parentId: request.ParentId
            );
            
            await repository.Create(newCategory);
            return Envelope<ProductCategoryDto>.Ok(newCategory.ToDto());
        }, cancellationToken);
    }
}