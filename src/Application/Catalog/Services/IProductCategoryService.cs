using System.Linq.Expressions;
using Application.Catalog.Dtos;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Domain.Catalog;

namespace Application.Catalog.Services;

public interface IProductCategoryService
{
    Task<Envelope<ProductCategoryDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Envelope<ProductCategoryDto>> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Envelope<IEnumerable<ProductCategoryDto>>> GetAllAsync(
        string? query = null,
        SortOption? sort = null,
        int? take = null,
        CancellationToken ct = default);
    
    Task<Envelope<ProductCategoryDto>> AddAsync(ProductCategoryCreateDto dto, CancellationToken ct = default);
    Task<bool> ExistAsync(Expression<Func<ProductCategory, bool>> predicate, CancellationToken ct = default);
    
    Task<IEnumerable<ProductCategoryDto>> GetAllAsync(
        Expression<Func<ProductCategory, bool>>? predicate = null, 
        SortOption? sort = null, 
        int? take = null, 
        bool asNoTracking = true,
        CancellationToken ct = default);
}