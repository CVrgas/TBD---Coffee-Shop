using Application.Catalog.Dtos;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Paginated;
using MediatR;

namespace Application.Catalog.Queries.GetCategoriesPaginated;

public sealed record GetCategoriesPaginatedQuery(PaginatedRequest Request) : IRequest<Envelope<Paginated<ProductCategoryDto>>>;
