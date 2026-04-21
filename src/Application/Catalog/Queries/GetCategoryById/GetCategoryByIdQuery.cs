using Application.Catalog.Dtos;
using Application.Common.Abstractions.Envelope;
using MediatR;

namespace Application.Catalog.Queries.GetCategoryById;

public sealed record GetCategoryByIdQuery(int Id) : IRequest<Envelope<ProductCategoryDto>>;
