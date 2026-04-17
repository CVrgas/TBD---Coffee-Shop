using Application.Common.Interfaces;
using Domain.Catalog;

namespace Application.Catalog.Specifications;

public class CategoriesSlugsSpec(IEnumerable<string> slugs) : Specification<ProductCategory>(c => slugs.Contains(c.Slug));