using Application.Catalog.Dtos;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Paginated;
using MediatR;

namespace Application.Catalog.Queries.GetProductsByCategory;

public sealed record GetProductsByCategoryQuery(PaginatedRequest Request) : IRequest<Envelope<Paginated<ProductDto>>>;
