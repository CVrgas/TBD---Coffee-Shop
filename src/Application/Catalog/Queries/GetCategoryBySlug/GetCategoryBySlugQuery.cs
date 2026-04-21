using Application.Catalog.Dtos;
using Application.Common.Abstractions.Envelope;
using MediatR;

namespace Application.Catalog.Queries.GetCategoryBySlug;

public sealed record GetCategoryBySlugQuery(string Slug) : IRequest<Envelope<ProductCategoryDto>>;
