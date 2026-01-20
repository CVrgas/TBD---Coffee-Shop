using Application.Common;
using Application.Common.Abstractions.Persistence.Paginated;

namespace Application.Catalog.Dtos;

public sealed record ProductQuery(string? Query, int? Take) : SortOptionsBase;

public sealed record ProductPaginatedQuery() : PaginatedRequestBase;