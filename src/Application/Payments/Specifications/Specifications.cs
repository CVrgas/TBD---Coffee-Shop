using Application.Common.Interfaces;
using Domain.Base;
using Domain.Orders;

namespace Application.Payments.Specifications;

internal class OrderWithPaymentsSpec(string orderNumber) : Specification<Order>(o => o.OrderNumber == orderNumber);
internal class PaymentByIntentIdSpec(string intentId, IEnumerable<PaymentStatus>? status = null) : Specification<PaymentRecord>(record => record.IntentId == intentId && (status == null || status.Contains(record.Status)));
internal class PaymentByOrderIdSpec(int orderId, IEnumerable<PaymentStatus>? status = null) : Specification<PaymentRecord>(record => record.OrderId == orderId && (status == null || status.Contains(record.Status)));