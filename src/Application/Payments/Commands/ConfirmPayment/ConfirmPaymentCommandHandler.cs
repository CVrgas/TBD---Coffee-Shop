using Application.Common.Abstractions.Envelope;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Payment;
using Domain.Base.Enum;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Payments.Commands.ConfirmPayment;

internal sealed class ConfirmPaymentCommandHandler(IAppDbContext context, IPaymentGateway gateway) : IRequestHandler<ConfirmPaymentCommand, Envelope>
{
    public async Task<Envelope> Handle(ConfirmPaymentCommand request, CancellationToken cancellationToken)
    {
        return await context.ExecuteInTransactionAsync(async ct =>
        {
            var paymentRecord = await context.PaymentRecords
                .Where(pr => pr.IntentId == request.IntentId)
                .FirstOrDefaultAsync(ct);
            
            if (paymentRecord is null) 
                return Envelope.NotFound("Payment record not found.");
                
            if (paymentRecord.Status != PaymentStatus.Created)
                return Envelope.BadRequest("Payment is not in a confirmable state.");

            var order = await context.Orders
                .Where(o => o.Id == paymentRecord.OrderId)
                .FirstOrDefaultAsync(ct);
            
            if (order is null) 
                return Envelope.NotFound("Associated order not found.");

            var confirmation = await gateway.ConfirmPaymentAsync(paymentRecord.IntentId, ct);

            if (confirmation.IsSuccess)
            {
                paymentRecord.ChangePaymentStatus(PaymentStatus.Approved);
                
                var totalPaidInRecords = await context.PaymentRecords
                    .AsNoTracking()
                    .Where(pr => pr.OrderId == order.Id && pr.Status == PaymentStatus.Approved)
                    .SumAsync(r => r.Amount, ct);
                
                // Incorporate the current record's amount as it is newly approved but not yet committed to DB
                var totalPaid = totalPaidInRecords + paymentRecord.Amount;

                if (totalPaid >= order.Total)
                {
                    order.UpdateStatus(OrderStatus.Confirmed);
                    context.Orders.Update(order);
                }
            }
            else
            {
                paymentRecord.ChangePaymentStatus(PaymentStatus.Failed);
            }

            context.PaymentRecords.Update(paymentRecord);

            return confirmation.IsSuccess
                ? Envelope.Ok()
                : Envelope.BadRequest(confirmation.ErrorMessage ?? "Payment failed at gateway.");
            
        }, cancellationToken);
    }
}