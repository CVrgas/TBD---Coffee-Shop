using Application.Catalog.Dtos;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Extensions;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Queries.GetCategoriesPaginated;

public class GetCategoriesPaginatedQueryHandler(IAppDbContext context) : IRequestHandler<GetCategoriesPaginatedQuery, Envelope<Paginated<ProductCategoryDto>>>
{
    public async Task<Envelope<Paginated<ProductCategoryDto>>> Handle(GetCategoriesPaginatedQuery request, CancellationToken cancellationToken)
    {
        var queryable = context.ProductCategories.AsNoTracking()
            .Where(c => string.IsNullOrWhiteSpace(request.Request.QueryPattern) || c.Name.Contains(request.Request.QueryPattern))
            .ApplySort(request.Request.SortOption);
        
        var totalCount = await queryable.CountAsync(cancellationToken);
        
        queryable = queryable.Skip(request.Request.Skip).Take(request.Request.PageSize!.Value);

        var categories = await queryable.Select(CategoryMappingExtensions.CategoryDtoProjection).ToListAsync(cancellationToken: cancellationToken);
        return Envelope<Paginated<ProductCategoryDto>>.Ok(new Paginated<ProductCategoryDto>(Entities: categories, TotalCount: totalCount, PageNumber: request.Request.PageIndex!.Value, PageSize: request.Request.PageSize.Value));
    }
}
