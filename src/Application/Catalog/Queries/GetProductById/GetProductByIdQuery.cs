using Application.Catalog.Dtos;
using Application.Common.Abstractions.Envelope;
using MediatR;

namespace Application.Catalog.Queries.GetProductById;

public sealed record GetProductByIdQuery(int Id) : IRequest<Envelope<ProductDto>>;
