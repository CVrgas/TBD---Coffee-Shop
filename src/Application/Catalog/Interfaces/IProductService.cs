using Application.Catalog.Dtos;
using Application.Common.Abstractions.Envelope;

namespace Application.Catalog.Interfaces;

public interface IProductService
{
    Task<Envelope<ProductDto>> AddAsync(ProductCreateDto product, CancellationToken ct = default);
    Task<Envelope<ProductDto>> UpdateAsync(ProductUpdateDto dto, CancellationToken ct = default);
    Task<Envelope> RateProductAsync(int productId, int rate, CancellationToken ct = default);
    Task<Envelope> ActiveProduct(int productId, CancellationToken ct = default);
    Task<Envelope> DeactiveProduct(int productId, CancellationToken ct = default);
    Task<Envelope> ToggleStatus(int productId, bool? state = null, CancellationToken ct = default);
    Task<Envelope> UpdatePrice(ProductUpdatePrice updatePrice, CancellationToken ct= default);
    Task<Envelope> UpdateImageAsync(int id, string imageUrl, CancellationToken ct = default);
    Task<Envelope> BulkCreateAsync(List<ProductCreateDto> dtos, CancellationToken ct = default);
}