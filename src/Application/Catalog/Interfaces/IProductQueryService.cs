using Application.Catalog.Dtos;
using Application.Common.Abstractions.Persistence;

namespace Application.Catalog.Interfaces;

public interface IProductQueryService
{
    Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    //Task<ProductDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<Paginated<ProductDto>> GetByCategoryAsync(string category, int pageSize = 10, int pageNumber = 0, CancellationToken cancellationToken = default);
    Task<Paginated<ProductDto>> PaginatedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}