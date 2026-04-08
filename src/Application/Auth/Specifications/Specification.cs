using Application.Common.Interfaces;
using Domain.Catalog;
using Domain.User;

namespace Application.Auth.Specifications;

public class ExistEmailSpec(string email): Specification<User>(u => u.IsActive && email == u.Email.Value);
public class ProductNameSpec(string name, int? productId = null) : Specification<Product>(p => p.Name == name && (productId == null || p.Id == productId));
public class CategoriesByIdsSpec(IEnumerable<int> ids) : Specification<ProductCategory>(c => ids.Contains(c.Id));
