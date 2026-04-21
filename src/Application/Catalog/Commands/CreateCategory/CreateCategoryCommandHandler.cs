using Application.Catalog.Dtos;
using Application.Catalog.Mapping;
using Application.Common;
using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces;
using Domain.Catalog;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Commands.CreateCategory;

public class CreateCategoryCommandHandler(IAppDbContext context) : IRequestHandler<CreateCategoryCommand,  Envelope<ProductCategoryDto>>
{
    public async Task<Envelope<ProductCategoryDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        return await context.ExecuteInTransactionAsync(async _ =>
        {
            if(string.IsNullOrWhiteSpace(request.Code)) return Envelope<ProductCategoryDto>.BadRequest()
                .WithError(nameof(request.Code), "Category Code is required.");
            
            var name = request.Name!.Trim();
            var slug = name.Slugify();

            var x = await context.ProductCategories
                .Where(c => 
                    (!request.ParentId.HasValue || c.Id == request.ParentId.Value) 
                    && c.Slug == slug)
                .ToDictionaryAsync(c => c.Id, c => new { c.IsActive, c.Slug }, cancellationToken);
            
            if (request.ParentId.HasValue && x.TryGetValue(request.ParentId.Value, out var sl) && !sl.IsActive)
            {
                return Envelope<ProductCategoryDto>.BadRequest()
                    .WithError(nameof(request.ParentId), "Parent category is inactive.");
            }
            if (x.Any(c => c.Value.Slug == slug))
            {
                return Envelope<ProductCategoryDto>.BadRequest()
                    .WithError(nameof(request.Name), "Category name already exists.");
            }

            var newCategory = ProductCategory.Create(
                name,
                slug,
                code: request.Code,
                description: request.Description,
                parentId: request.ParentId
            );
            
            await context.ProductCategories.AddAsync(newCategory, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Envelope<ProductCategoryDto>.Ok(newCategory.ToDto());
        }, cancellationToken);
    }
}