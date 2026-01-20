using System.Linq.Expressions;
using Application.Common.Interfaces;
using Domain.Orders;

namespace Application.Orders.Specifications;

public class OrderWithPaymentsSpec(string orderNumber) : Specification<Order>(o => o.OrderNumber == orderNumber);