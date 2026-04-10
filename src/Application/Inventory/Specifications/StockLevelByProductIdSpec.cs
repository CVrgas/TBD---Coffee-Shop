using Application.Common.Interfaces;
using Domain.Inventory;

namespace Application.Inventory.Specifications;

public class StockLevelByProductIdSpec(int productId) : Specification<StockItem>(s => s.ProductId == productId && s.IsActive);