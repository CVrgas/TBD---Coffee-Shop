namespace Application.Catalog.Dtos;

public class ProductCategoryCreateDto
{
    private string _name = string.Empty;

    public string Name
    {
        get => _name; 
        set => _name = value.Trim();
    }

    private string _code = string.Empty;
    public string Code
    {
        get => _code; 
        set => _code = value.ToUpperInvariant().Replace(" ", "").Trim();
    } 
    public string? Description { get; set; } 
    public int? ParentId { get; set; }
};