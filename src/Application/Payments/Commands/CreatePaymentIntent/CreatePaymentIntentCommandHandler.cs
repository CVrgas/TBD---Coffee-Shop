using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Application.Common.Interfaces.Payment;
using Application.Payments.Specifications;
using Domain.Base;
using Domain.Orders;
using MediatR;

namespace Application.Payments.Commands.CreatePaymentIntent;

internal sealed class CreatePaymentIntentCommandHandler(
    IRepository<Order, int> orderRepository, 
    IRepository<PaymentRecord, int> paymentRepository, 
    IPaymentGateway gateway,
    IUnitOfWork uOw) : IRequestHandler<CreatePaymentIntentCommand, Envelope<PaymentConfirmationResult>>
{
    public async Task<Envelope<PaymentConfirmationResult>> Handle(CreatePaymentIntentCommand request, CancellationToken cancellationToken)
    {
        return await uOw.ExecuteInTransactionAsync(async _ =>
        {
            var order = await orderRepository.GetAsync(new OrderWithPaymentsSpec(request.OrderNumber), ct: cancellationToken);
        
            if(order is null) return Envelope<PaymentConfirmationResult>.NotFound("order not found.");
            if(order.Status == OrderStatus.Cancelled) return Envelope<PaymentConfirmationResult>.NotFound("order cancelled.");

            var records = await paymentRepository.ListAsync(new PaymentByOrderIdSpec(order.Id), ct: cancellationToken);
            if (records.Any())
            {
                var record = records
                    .FirstOrDefault(r => r.Status is PaymentStatus.Created);
            
                if(record is not null)
                {
                    var result = new PaymentConfirmationResult(record.IntentId, record.ClientSecret, true);
                    return Envelope<PaymentConfirmationResult>.Ok(result);
                }
            }

            var duePayment = order.Total - records.Where(p => p.Status == PaymentStatus.Approved).Sum(r => r.Amount);
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
        
            await paymentRepository.Create(newRecord);
            return  Envelope<PaymentConfirmationResult>.Ok(
                new PaymentConfirmationResult(
                    newRecord.IntentId,
                    intentResult.ClientSecret,
                    true));
            
        }, cancellationToken);
    }
}