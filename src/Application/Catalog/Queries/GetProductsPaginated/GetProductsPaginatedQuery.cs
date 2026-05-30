using Application.Catalog.Dtos;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Paginated;
using MediatR;

namespace Application.Catalog.Queries.GetProductsPaginated;

public sealed record GetProductsPaginatedQuery(PaginatedRequest Request) : IRequest<Envelope<Paginated<ProductDto>>>;
