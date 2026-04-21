using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Commands.UpdatePrice;

public class UpdatePriceCommandHandler(IAppDbContext context) : IRequestHandler<UpdatePriceCommand, Envelope>
{
    public async Task<Envelope> Handle(UpdatePriceCommand request, CancellationToken cancellationToken)
    {
        return await context.ExecuteInTransactionAsync(async _ =>
        {
            var product = await context.Products.FirstOrDefaultAsync(p => p.IsActive && p.Id == request.Id, cancellationToken: cancellationToken);
            if (product is null) 
                return Envelope.NotFound("Product not found.");
        
            product.UpdatePrice(request.Price, request.FormatCurrency);
            return Envelope.Ok();
        }, cancellationToken);
    }
}