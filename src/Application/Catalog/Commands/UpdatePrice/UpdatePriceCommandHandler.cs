using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Domain.Catalog;
using MediatR;

namespace Application.Catalog.Commands.UpdatePrice;

public class UpdatePriceCommandHandler(
    IRepository<Product, int> repository,
    IUnitOfWork uOw) : IRequestHandler<UpdatePriceCommand, Envelope>
{
    public async Task<Envelope> Handle(UpdatePriceCommand request, CancellationToken cancellationToken)
    {
        return await uOw.ExecuteInTransactionAsync(async _ =>
        {
            var product = await repository.GetByIdAsync(request.Id, asNoTracking:false, ct: cancellationToken);
            if (product is null) 
                return Envelope.NotFound("Product not found.");
        
            product.UpdatePrice(request.Price, request.FormatCurrency);
            return Envelope.Ok();
        }, cancellationToken);
    }
}