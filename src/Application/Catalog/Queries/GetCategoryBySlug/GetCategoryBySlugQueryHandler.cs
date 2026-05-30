using Application.Catalog.Dtos;
using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Queries.GetCategoryBySlug;

public class GetCategoryBySlugQueryHandler(IAppDbContext context) : IRequestHandler<GetCategoryBySlugQuery, Envelope<ProductCategoryDto>>
{
    public async Task<Envelope<ProductCategoryDto>> Handle(GetCategoryBySlugQuery request, CancellationToken cancellationToken)
    {
        var category = await context.ProductCategories.AsNoTracking()
            .Where(c => c.Slug == request.Slug)
            .Select(CategoryMappingExtensions.CategoryDtoProjection)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
        
        if (category == null) return Envelope<ProductCategoryDto>.NotFound();
        
        var breadcrumbs = await CategoryMappingExtensions.BuildBreadCrumbAsync(context, category.Id, cancellationToken);
        var result = category with { Breadcrumb = breadcrumbs };
        
        return Envelope<ProductCategoryDto>.Ok(result);
    }
}
