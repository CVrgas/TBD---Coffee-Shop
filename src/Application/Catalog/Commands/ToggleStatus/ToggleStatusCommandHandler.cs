using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace Application.Catalog.Commands.ToggleStatus;

public class ToggleStatusCommandHandler(IAppDbContext context) : IRequestHandler<ToggleStatusCommand, Envelope>
{
    public async Task<Envelope> Handle(ToggleStatusCommand request, CancellationToken cancellationToken)
    {
        return await context.ExecuteInTransactionAsync(async _ =>
        {
            if (request.ProductId <= 0)
                return Envelope.BadRequest().WithError("product id", "Invalid product id, need to be positive.");

            var product = await context.Products.FirstOrDefaultAsync(p => p.IsActive && p.Id == request.ProductId, cancellationToken: cancellationToken);
            if (product is null) return Envelope.NotFound("Product not found.");

            product.ToggleStatus(request.State);
            return Envelope.Ok();
        }, cancellationToken);
    }
}