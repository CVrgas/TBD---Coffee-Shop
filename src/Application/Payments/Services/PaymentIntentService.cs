using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Payment;
using Domain.Base;
using Domain.Orders;

namespace Application.Payments.Services;

public class PaymentIntentService(
    IRepository<Order, int> orderRepository, 
    IRepository<PaymentRecord, int> payRecordRepository, 
    IPaymentGateway gateway,
    IUnitOfWork uOw) : IPaymentIntentService
{
    public async Task<Envelope<PaymentConfirmationResult>> CreatePaymentIntentAsync(string orderNumber, CancellationToken ct = default)
    {
        return await uOw.ExecuteInTransactionAsync(async _ =>
        {
            var order = await orderRepository.GetAsync(new OrderWithPaymentsSpec(orderNumber), ct: ct);
        
            if(order is null) return Envelope<PaymentConfirmationResult>.NotFound("order not found.");
            if(order.Status == OrderStatus.Cancelled) return Envelope<PaymentConfirmationResult>.NotFound("order cancelled.");

            if (order.PaymentRecords.Any())
            {
                var record = order.PaymentRecords
                    .FirstOrDefault(r => r.Status is PaymentStatus.Authorized or PaymentStatus.Pending);
            
                if(record is not null)
                {
                    var result = new PaymentConfirmationResult(record.IntentId, record.ClientSecret, true);
                    return Envelope<PaymentConfirmationResult>.Ok(result);
                }
            }

            var duePayment = order.Total - order.PaymentRecords.Where(p => p.Status == PaymentStatus.Paid).Sum(r => r.Amount);
            if(duePayment <= 0) return Envelope<PaymentConfirmationResult>.BadRequest("no payment due.");
            
            var intentResult = await gateway.CreateIntentAsync(order.OrderNumber, duePayment, order.Currency.Code, ct);

            var newRecord = new PaymentRecord
            {
                Amount = duePayment,
                Currency = order.Currency,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                IntentId = intentResult.IntentId,
                ClientSecret = intentResult.ClientSecret,
                OrderId = order.Id,
                Provider = PaymentProvider.Mock0,
            };
        
            await payRecordRepository.Create(newRecord);
            return  Envelope<PaymentConfirmationResult>.Ok(
                new PaymentConfirmationResult(
                    newRecord.IntentId,
                    intentResult.ClientSecret,
                    true));
            
        }, ct);

    }

    public async Task<Envelope> ConfirmPaymentIntentAsync(string intentId, string orderNumer, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(intentId)) return Envelope.BadRequest("invalid intent id");
        
        return await uOw.ExecuteInTransactionAsync(async _ =>
        {
            var record = await payRecordRepository.GetAsync(new PaymentConfirmationSpec(intentId), asNoTracking: false, ct: ct);
            if (record is null) return Envelope.NotFound("intent not found.");

            if (!(record.Order.OrderNumber == orderNumer && record.IntentId == intentId))
                return Envelope.BadRequest("invalid order number.");
            if (record.Status == PaymentStatus.Paid) return Envelope.Ok();

            var confirmation = await gateway.ConfirmPaymentAsync(record.IntentId, ct);
            if (confirmation.IsSuccess)
            {
                record.Status = PaymentStatus.Paid;

                var totalPaid = record.Order.PaymentRecords
                    .Where(r => r.Status == PaymentStatus.Paid)
                    .Sum(r => r.Amount);

                if (totalPaid >= record.Order.Total) record.Order.UpdateStatus(OrderStatus.Confirmed);
            }
            else
            {
                record.Status = PaymentStatus.Declined;
            }

            return confirmation.IsSuccess
                ? Envelope.Ok()
                : Envelope.BadRequest(confirmation.ErrorMessage ?? "unknown error");
            
        }, ct);
    }
}

internal class OrderWithPaymentsSpec(string orderNumber) : Specification<Order>(o => o.OrderNumber == orderNumber);
internal class PaymentConfirmationSpec : Specification<PaymentRecord>
{
    public PaymentConfirmationSpec(string intentId) : base(record => record.IntentId == intentId) 
    {
        AddInclude(record => record.Order);
        AddInclude(record => record.Order.PaymentRecords);
    }
};