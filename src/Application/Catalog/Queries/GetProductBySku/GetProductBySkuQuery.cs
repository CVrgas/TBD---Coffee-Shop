using Application.Catalog.Dtos;
using Application.Common.Abstractions.Envelope;
using MediatR;

namespace Application.Catalog.Queries.GetProductBySku;

public sealed record GetProductBySkuQuery(string Sku) : IRequest<Envelope<ProductDto>>;
