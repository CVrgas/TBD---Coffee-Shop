namespace Application.Common.Abstractions.Persistence;

public record Paginated<T>(IEnumerable<T> Entities, int TotalCount, int PageNumber = 1, int PageSize = 10)
{
    public IEnumerable<T> Entities { get; set; } = Entities;
    public int PageNumber { get; set; } = PageNumber;
    public int PageSize { get; set; } = PageSize;
    public int TotalCount { get; set; } = TotalCount;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
};