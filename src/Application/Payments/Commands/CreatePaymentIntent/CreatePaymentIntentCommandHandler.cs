using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Payment;
using Domain.Base.Enum;
using Domain.Orders.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Payments.Commands.CreatePaymentIntent;

internal sealed class CreatePaymentIntentCommandHandler(IAppDbContext context, IPaymentGateway gateway) : IRequestHandler<CreatePaymentIntentCommand, Envelope<PaymentConfirmationResult>>
{
    public async Task<Envelope<PaymentConfirmationResult>> Handle(CreatePaymentIntentCommand request, CancellationToken cancellationToken)
    {
        return await context.ExecuteInTransactionAsync(async _ =>
        {
            var order = await context.Orders
                .Where(o => o.OrderNumber == request.OrderNumber)
                .GroupJoin(context.PaymentRecords, o => o.Id, p => p.OrderId, (o, payments) => new
                {
                    o.Id,
                    o.OrderNumber,
                    o.Total,
                    o.Currency,
                    o.Status,
                    records = payments.Select(p => new
                    {
                        p.Amount,
                        p.Currency,
                        p.IntentId,
                        p.ClientSecret,
                        p.Status
                    }).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);
        
            if(order is null) return Envelope<PaymentConfirmationResult>.NotFound("order not found.");
            if(order.Status == OrderStatus.Cancelled) return Envelope<PaymentConfirmationResult>.NotFound("order cancelled.");
            
            if (order.records.Any())
            {
                var record = order.records
                    .FirstOrDefault(r => r.Status is PaymentStatus.Created);
            
                if(record is not null)
                {
                    var result = new PaymentConfirmationResult(record.IntentId, record.ClientSecret, true);
                    return Envelope<PaymentConfirmationResult>.Ok(result);
                }
            }

            var duePayment = order.Total - order.records.Where(p => p.Status == PaymentStatus.Approved).Sum(r => r.Amount);
            if(duePayment <= 0) return Envelope<PaymentConfirmationResult>.BadRequest("no payment due.");
            
            var intentResult = await gateway.CreateIntentAsync(order.OrderNumber, duePayment, order.Currency.Code, cancellationToken);

            var newRecord = PaymentRecord.Create(
                orderId: order.Id,
                provider: PaymentProvider.Mock0,
                amount: duePayment,
                currency: order.Currency,
                intentId: intentResult.IntentId,
                clientSecret: intentResult.ClientSecret,
                paymentStatus: PaymentStatus.Created
            );
        
            await context.PaymentRecords.AddAsync(newRecord, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return  Envelope<PaymentConfirmationResult>.Ok(
                new PaymentConfirmationResult(
                    newRecord.IntentId,
                    intentResult.ClientSecret,
                    true));
            
        }, cancellationToken);
    }
}