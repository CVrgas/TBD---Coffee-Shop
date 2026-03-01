using System.ComponentModel.DataAnnotations;

namespace Application.Catalog.Dtos;

public sealed record ProductCreateDto
{
    private string _name = string.Empty;

    public string Name
    {
        get => _name;
        private set =>  _name = value.Trim();
    }
    
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = null!;
}