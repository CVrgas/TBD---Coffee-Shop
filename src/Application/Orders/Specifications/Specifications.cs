using Application.Common.Interfaces;
using Domain.Base;
using Domain.Catalog;
using Domain.Orders;

namespace Application.Orders.Specifications;

public class ProductByIds(IEnumerable<int> productIds) : Specification<Product>(p => productIds.Contains(p.Id));
public class GetOrderCancelSpec(string orderNumber) : Specification<Order>(order => order.OrderNumber == orderNumber && order.Status == OrderStatus.Pending);