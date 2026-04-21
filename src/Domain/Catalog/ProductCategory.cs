using Domain.Base.Entities;

namespace Domain.Catalog;

public sealed class ProductCategory : Entity<int>
{
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set;  } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get;  private set; }
    public int? ParentId { get; private set; }
    public ProductCategory? Parent { get; private set; }
    public List<ProductCategory> Children { get; private set; } = [];
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    public IEnumerable<Product>? Products { get; private set; }
    
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
