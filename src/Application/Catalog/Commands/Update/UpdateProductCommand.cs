using Application.Catalog.Dtos;
using Application.Common.Abstractions.Envelope;
using MediatR;

namespace Application.Catalog.Commands.Update;

public class UpdateProductCommand: IRequest<Envelope<ProductDto>>
{
    public int ProductId { get; private set; } 
    public string? Name { get; init; } 
    public string? Description { get; init; } 
    public int? CategoryId { get; init; } 
    byte[]? RowVersion { get; init; }

    public void SetProductId(int productId)
    {
        this.ProductId = productId;
    }
};