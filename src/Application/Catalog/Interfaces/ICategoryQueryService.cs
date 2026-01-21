using Application.Catalog.Dtos;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Paginated;

namespace Application.Catalog.Interfaces;

public interface ICategoryQueryService
{
    Task<ProductCategoryDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ProductCategoryDto?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Paginated<ProductCategoryDto>> GetAllAsync(
        PaginatedRequest pagination,
        CancellationToken ct = default);
}