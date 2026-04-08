using Application.Common.Interfaces;
using Domain.Inventory;

namespace Application.Inventory.Specifications;


public class AdjustSpec(int productId) : Specification<StockItem>(item => item.IsActive && item.ProductId == productId);