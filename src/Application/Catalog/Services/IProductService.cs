using Application.Catalog.Dtos;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Paginated;
using Application.Common.Abstractions.Persistence.Repository;
using Domain.Catalog;


namespace Application.Catalog.Services;

public interface IProductService
{
    Task<Envelope<ProductDto>> AddAsync(ProductCreateDto product, CancellationToken ct = default);
    
    Task<Envelope<ProductDto>> GetProductByIdAsync(int id, CancellationToken ct = default);
    Task<Envelope<ProductDto>> GetProductBySkuAsync(string sku, CancellationToken ct = default);
    
    Task<Envelope<IEnumerable<ProductDto>>> GetAllAsync(
        string? query = null, SortOption? sort = null, int? take = null, CancellationToken ct = default);
    
    Task<Envelope<Paginated<ProductDto>>> GetPaginatedAsync(ProductPaginatedQuery request, CancellationToken ct = default);
    
    Task<Envelope<Paginated<ProductDto>>> GetPaginatedByCategoryAsync(
        string categorySlug, PaginatedRequest request, CancellationToken ct = default);

    Task<Envelope<ProductDto>> UpdateAsync(ProductUpdateDto dto, CancellationToken ct = default);
    Task<Envelope> RateProductAsync(int productId, int rate, CancellationToken ct = default);
    Task<Envelope> ActiveProduct(int productId, CancellationToken ct = default);
    Task<Envelope> DeactiveProduct(int productId, CancellationToken ct = default);
    Task<Envelope> ToggleStatus(int productId, bool? state = null, CancellationToken ct = default);
    Task<Envelope> UpdatePrice(ProductUpdatePrice updatePrice, CancellationToken ct= default);
    Task<Envelope> UpdateImageAsync(int id, string imageUrl, CancellationToken ct = default);
    Task<Envelope> BulkCreateAsync(List<ProductCreateDto> dtos, CancellationToken ct = default);
    Task<Envelope<string>> GetFilters();
}