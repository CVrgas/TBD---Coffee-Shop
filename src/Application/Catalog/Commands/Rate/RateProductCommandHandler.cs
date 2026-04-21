using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Commands.Rate;

public class RateProductCommandHandler(IAppDbContext context) : IRequestHandler<RateProductCommand, Envelope>
{
    public async Task<Envelope> Handle(RateProductCommand request, CancellationToken cancellationToken)
    {
        return await context.ExecuteInTransactionAsync(async _ =>
        {
            var product = await context.Products.FirstOrDefaultAsync(p => p.IsActive && p.Id == request.ProductId, cancellationToken: cancellationToken);
            if (product is null) return Envelope.NotFound("Product not found.");

            product.AddRating(request.Rate);
            
            return Envelope.Ok();
        }, cancellationToken);
    }
}