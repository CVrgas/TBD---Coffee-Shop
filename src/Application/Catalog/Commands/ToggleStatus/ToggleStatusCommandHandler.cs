using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Domain.Catalog;
using MediatR;

namespace Application.Catalog.Commands.ToggleStatus;

public class ToggleStatusCommandHandler(
    IRepository<Product, int> repository,
    IUnitOfWork uOw) : IRequestHandler<ToggleStatusCommand, Envelope>
{
    public async Task<Envelope> Handle(ToggleStatusCommand request, CancellationToken cancellationToken)
    {
        return await uOw.ExecuteInTransactionAsync(async _ =>
        {
            if (request.ProductId <= 0)
                return Envelope.BadRequest().WithError("product id", "Invalid product id, need to be positive.");

            var product = await repository.GetByIdAsync(request.ProductId, asNoTracking: false, ct: cancellationToken);
            if (product is null) return Envelope.NotFound("Product not found.");

            product.ToggleStatus(request.State);
            return Envelope.Ok();
        }, cancellationToken);
    }
}