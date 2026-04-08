using Application.Catalog.Dtos;
using Application.Common.Abstractions.Envelope;
using MediatR;

namespace Application.Catalog.Commands.Create;

public sealed record CreateProductCommand() : IRequest<Envelope<ProductDto>>
{
    private string _name = string.Empty;

    public string Name
    {
        get => _name;
        set =>  _name = value.Trim();
    }
    
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = null!;
};