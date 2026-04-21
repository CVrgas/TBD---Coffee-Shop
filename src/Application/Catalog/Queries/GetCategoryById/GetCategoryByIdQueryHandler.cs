using Application.Catalog.Dtos;
using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Queries.GetCategoryById;

public class GetCategoryByIdQueryHandler(IAppDbContext context) : IRequestHandler<GetCategoryByIdQuery, Envelope<ProductCategoryDto>>
{
    public async Task<Envelope<ProductCategoryDto>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await context.ProductCategories.AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(CategoryMappingExtensions.CategoryDtoProjection)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
        
        if (category == null) return Envelope<ProductCategoryDto>.NotFound();
        
        var breadcrumbs = await CategoryMappingExtensions.BuildBreadCrumbAsync(context, category.Id, cancellationToken);
        var result = category with { Breadcrumb = breadcrumbs };
        
        return Envelope<ProductCategoryDto>.Ok(result);
    }
}
