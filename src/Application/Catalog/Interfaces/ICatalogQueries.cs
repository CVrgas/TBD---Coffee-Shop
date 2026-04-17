using Application.Catalog.Dtos;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Paginated;
using Application.Inventory.Dtos;

namespace Application.Catalog.Interfaces;

public interface ICatalogQueries
{
    Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<Paginated<ProductDto>> PaginatedAsync(PaginatedRequest request, CancellationToken cancellationToken = default);
    Task<Paginated<ProductDto>> GetByCategoryAsync(PaginatedRequest request, CancellationToken cancellationToken = default);
    Task<List<StockItemDto>> GetStockItemsAsync(int productId, CancellationToken ct = default);
}