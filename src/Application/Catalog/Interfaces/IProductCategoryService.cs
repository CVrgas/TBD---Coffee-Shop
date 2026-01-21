using Application.Catalog.Dtos;
using Application.Common.Abstractions.Envelope;

namespace Application.Catalog.Interfaces;

public interface IProductCategoryService
{
    Task<Envelope<ProductCategoryDto>> AddAsync(ProductCategoryCreateDto dto, CancellationToken ct = default);
}