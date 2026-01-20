using System.ComponentModel.DataAnnotations;

namespace Application.Catalog.Dtos;

public sealed record ProductCreateDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
    public decimal Price { get; set; }
    public string? Currency { get; set; }
}