using Application.Catalog.Dtos;
using Application.Catalog.Interfaces;
using Application.Catalog.Mapping;
using Application.Common;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Application.Common.Interfaces;
using Domain.Catalog;

namespace Application.Catalog.Services;

internal class CategoriesSlugsSpec(IEnumerable<string> slugs) : Specification<ProductCategory>(c => slugs.Contains(c.Slug));
public class ProductCategoryService (IRepository<ProductCategory, int> repository, IUnitOfWork uoW) : IProductCategoryService
{
    public async Task<Envelope<ProductCategoryDto>> AddAsync(ProductCategoryCreateDto dto, CancellationToken ct = default)
    {
        return await uoW.ExecuteInTransactionAsync(async _ =>
        {
            var name = dto.Name!.Trim();
            var slug = name.Slugify();

            if (dto.ParentId.HasValue)
            {
                var parent = await repository.GetByIdAsync(dto.ParentId.Value, ct: ct);
                if (parent is null || !parent.IsActive) 
                    return Envelope<ProductCategoryDto>.BadRequest()
                        .WithError(nameof(dto.ParentId), "category not found or inactive.");
            }
            
            var exist = await repository.ExistsAsync(new CategoriesSlugsSpec([slug]), ct: ct);
            if (exist) 
                return Envelope<ProductCategoryDto>.BadRequest()
                    .WithError(nameof(dto.ParentId), "Category already exists.");
            
            if(string.IsNullOrWhiteSpace(dto.Code)) return Envelope<ProductCategoryDto>.BadRequest()
                .WithError(nameof(dto.Code), "Category Code is required.");

            var newCategory = ProductCategory.Create(
                name,
                slug,
                code: dto.Code,
                description: dto.Description,
                parentId: dto.ParentId
            );
            
            await repository.Create(newCategory);
            return Envelope<ProductCategoryDto>.Ok(newCategory.ToDto());
        }, ct);
    }
}