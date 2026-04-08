using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Application.Common.Interfaces.Payment;
using Application.Payments.Specifications;
using Domain.Base;
using Domain.Orders;
using MediatR;

namespace Application.Payments.Commands.ConfirmPayment;

internal sealed class ConfirmPaymentCommandHandler(
    IRepository<Order, int> orderRepository,
    IRepository<PaymentRecord, int> paymentRepository,
    IPaymentGateway gateway,
    IUnitOfWork uOw) : IRequestHandler<ConfirmPaymentCommand, Envelope>
{
    public async Task<Envelope> Handle(ConfirmPaymentCommand request, CancellationToken cancellationToken)
    {
        return await uOw.ExecuteInTransactionAsync(async ct =>
        {
            var paymentRecord = await paymentRepository.GetAsync(new PaymentByIntentIdSpec(request.IntentId), ct: ct);
            
            if (paymentRecord is null) 
                return Envelope.NotFound("Payment record not found.");
                
            if (paymentRecord.Status != PaymentStatus.Created)
                return Envelope.BadRequest("Payment is not in a confirmable state.");

            var order = await orderRepository.GetByIdAsync(paymentRecord.OrderId, ct: ct);
            if (order is null) 
                return Envelope.NotFound("Associated order not found.");

            var confirmation = await gateway.ConfirmPaymentAsync(paymentRecord.IntentId, ct);

            if (confirmation.IsSuccess)
            {
                paymentRecord.ChangePaymentStatus(PaymentStatus.Approved);
                
                var paidRecords = await paymentRepository.ListAsync(new PaymentByOrderIdSpec(order.Id, [PaymentStatus.Approved]), ct: ct);
                
                // Incorporate the current record's amount as it is newly approved but not yet committed to DB
                var totalPaid = paidRecords.Sum(r => r.Amount) + paymentRecord.Amount;

                if (totalPaid >= order.Total)
                {
                    order.UpdateStatus(OrderStatus.Confirmed);
                    orderRepository.Update(order);
                }
            }
            else
            {
                paymentRecord.ChangePaymentStatus(PaymentStatus.Failed);
            }

            paymentRepository.Update(paymentRecord);

            return confirmation.IsSuccess
                ? Envelope.Ok()
                : Envelope.BadRequest(confirmation.ErrorMessage ?? "Payment failed at gateway.");
            
        }, cancellationToken);
    }
}