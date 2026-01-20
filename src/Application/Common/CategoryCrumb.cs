using System.ComponentModel.DataAnnotations.Schema;

namespace Application.Common;

public sealed class CategoryCrumb
{
    [NotMapped]
    public int? ParentId { get; init; }
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
};