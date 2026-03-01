using Application.Common;
using Application.Common.Abstractions.Persistence.Paginated;

namespace Application.Orders.Dtos;

public class OrderPaginatedRequest : PaginatedRequestBase
{
    public int? UserId { get; set; }
    private string? _query;
    public string? Query { get => _query; set => _query = value?.Trim(); }
    public string? QueryPattern => !string.IsNullOrEmpty(Query) 
        ? $"%{Utilities.EscapeLike(Query)}%" 
        : null;
}