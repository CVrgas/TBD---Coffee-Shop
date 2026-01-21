namespace Application.Common.Abstractions.Persistence.Paginated;

public abstract record PaginatedRequestBase : SortOptionsBase
{
    private const int MaxPageSize = 100;
    private const int MinPageSize = 1;
    private const int MinPageIndex = 1;
    
    private string? _query;
    public string? Query { get => _query; set => _query = value?.Trim(); }
    public string? QueryPattern => !string.IsNullOrEmpty(Query) 
        ? $"%{Utilities.EscapeLike(Query)}%" 
        : null;
    
    public int PageIndex { get; set; } = 1;
    public int ClampIndex => Math.Clamp(PageIndex, MinPageIndex, int.MaxValue);
    public int PageSize { get; set; } = 10;
    public int ClampSize => Math.Clamp(PageSize, MinPageSize, MaxPageSize);
    public bool OnlyActive { get; set; } = true;
};

public record PaginatedRequest : PaginatedRequestBase;