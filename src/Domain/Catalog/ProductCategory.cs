using Domain.Base;

namespace Domain.Catalog;

public sealed class ProductCategory : Entity<int>
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set;  } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public ProductCategory? Parent { get; set; }
    public List<ProductCategory> Children { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public IEnumerable<Product>? Products { get; set; }
    
    private ProductCategory () {}

    public static ProductCategory Create(string name,string slug, string code, string? description = null, int? parentId = null)
    {
        return new ProductCategory
        {
            Name = name,
            Slug = slug,
            Code = code.ToUpperInvariant().Replace(" ", ""),
            Description = description,
            ParentId = parentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
    }
}
