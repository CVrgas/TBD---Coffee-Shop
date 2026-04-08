using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Domain.Catalog;
using MediatR;

namespace Application.Catalog.Commands.Rate;

public class RateProductCommandHandler(
    IRepository<Product, int> repository,
    IUnitOfWork uOw) 
    : IRequestHandler<RateProductCommand, Envelope>
{
    public async Task<Envelope> Handle(RateProductCommand request, CancellationToken cancellationToken)
    {
        return await uOw.ExecuteInTransactionAsync(async _ =>
        {
            var product = await repository.GetByIdAsync(request.ProductId, asNoTracking: false, ct: cancellationToken);
            if (product is null) return Envelope.NotFound("Product not found.");

            product.AddRating(request.Rate);
            
            return Envelope.Ok();
        }, cancellationToken);
    }
}