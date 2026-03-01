namespace Application.Common.Abstractions.Persistence.Paginated;

public abstract class PaginatedRequestBase : SortOptionsBase
{
    private const int MaxPageSize = 100;
    private const int MinPageSize = 1;
    private const int MinPageIndex = 1;
    
    private int _pageIndex = 1;
    private int _pageSize = 10;

    public int PageIndex
    {
        get => _pageIndex; 
        private set => Math.Clamp(value, MinPageIndex, int.MaxValue);
    }
    public int PageSize 
    { 
        get => _pageSize; 
        private set => Math.Clamp(value, MinPageSize, MaxPageSize); 
    }
    
    public int Skip => (PageIndex - 1) * PageSize;
    public bool OnlyActive { get; set; } = true;
};

public sealed class PaginatedRequest : PaginatedRequestBase
{
    private string? _query;
    public string? Query { get => _query; set => _query = value?.Trim(); }
    public string? QueryPattern => !string.IsNullOrEmpty(Query) 
        ? $"%{Utilities.EscapeLike(Query)}%" 
        : null;
};