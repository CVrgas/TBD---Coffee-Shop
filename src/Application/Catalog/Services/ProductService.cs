using System.Linq.Expressions;
using Application.Catalog.Dtos;
using Application.Catalog.Interfaces;
using Application.Catalog.Mapping;
using Application.Common;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Application.Common.Interfaces;
using Domain.Catalog;

namespace Application.Catalog.Services;

internal class ProductNameSpec(string name, int? productId = null) : Specification<Product>(p => p.Name == name && (productId == null || p.Id == productId));
internal class CategoriesByIdsSpec(IEnumerable<int> ids) : Specification<ProductCategory>(c => ids.Contains(c.Id));

public sealed class ProductService(
    IRepository<Product, int> repository, 
    IRepository<ProductCategory, int> categoryRepo,
    IUnitOfWork uOw) : IProductService
{
    #region Add Product
    
    public async Task<Envelope<ProductDto>> AddAsync(ProductCreateDto product, CancellationToken ct = default)
    {
        return await uOw.ExecuteInTransactionAsync(async (_) =>
        {
            var exist = await repository.ExistsAsync(new ProductNameSpec(product.Name), ct: ct);
            if (exist)
                return Envelope<ProductDto>.BadRequest()
                    .WithError(nameof(product.Name), "Product name already exists");

            var category = await categoryRepo.GetByIdAsync(id: product.CategoryId, ct: ct);

            if (category is null)
                return Envelope<ProductDto>.BadRequest("Category does not exist")
                    .WithError(nameof(product.CategoryId), "Category does not exist");

            var entity = Product.Create(
                name: product.Name,
                sku: $"{Utilities.GenerateSku(category.Code)}",
                price: product.Price,
                currencyCode: product.Currency,
                description: product.Description,
                imageUrl: product.ImageUrl,
                categoryId: product.CategoryId
            );

            await repository.Create(entity);
            await uOw.SaveChangesAsync(ct);
            return Envelope<ProductDto>.Ok(entity.ToDto() with { CategoryName = category.Name });
        }, ct);
    }
    
    public async Task<Envelope> BulkCreateAsync(List<ProductCreateDto> dtos, CancellationToken ct = default)
    {
        return await uOw.ExecuteInTransactionAsync(async _ =>
        {
            var incomingIds = dtos.Select(p => p.CategoryId).ToHashSet();
            var existingCategories = await categoryRepo.ListAsync(new CategoriesByIdsSpec(incomingIds), ct: ct);
            
            var categoryDictionary = existingCategories.ToDictionary(c=> c.Id, c=> c.Name);
            
            var missingIds = incomingIds.Where(id => !categoryDictionary.ContainsKey(id)).ToList();
            if (missingIds.Count != 0) 
                return Envelope.BadRequest()
                    .WithError("Categories", $"Missing categories: {string.Join(", ", missingIds)}");
            
            var products = new List<Product>();
            
            foreach (var dto in dtos)
            {
                var sku = Utilities.GenerateSku(categoryDictionary[dto.CategoryId]);
                products.Add(dto.ToEntity(sku));
            }
        
            await repository.CreateRange(products);
            return Envelope.Ok();
        }, ct); 

    }
    
    #endregion

    #region Update Product

    public async Task<Envelope<ProductDto>> UpdateAsync(ProductUpdateDto dto, CancellationToken ct = default)
    {
        return await uOw.ExecuteInTransactionAsync(async (_) =>
        {
            var product = await repository.GetByIdAsync(dto.ProductId, asNoTracking: false, ct: ct);
            if (product is null) return Envelope<ProductDto>.NotFound("Product not found.");
            
            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                var exist = await repository.ExistsAsync(new ProductNameSpec(dto.Name, dto.ProductId), ct: ct);
                if(exist) return Envelope<ProductDto>.BadRequest().WithError(nameof(product.Name),"Name already exists.");
            }
            
            if (dto.CategoryId.HasValue)
            {
                var categoryExist = await categoryRepo.ExistsAsync(new CategoriesByIdsSpec([dto.CategoryId.Value]), ct: ct);
                if(!categoryExist) return Envelope<ProductDto>.NotFound("Category does not exist.");
            }
            
            product.UpdateDetails(dto.Name, dto.Description, dto.CategoryId);
            
            return Envelope<ProductDto>.Ok(product.ToDto());
        }, ct: ct);
    }
    public async Task<Envelope> RateProductAsync(int productId, int rate, CancellationToken ct = default)
    {
        return await uOw.ExecuteInTransactionAsync(async _ =>
        {
            var product = await repository.GetByIdAsync(productId, asNoTracking: false, ct: ct);
            if (product is null) return Envelope.NotFound("Product not found.");

            product.AddRating(rate);
            
            return Envelope.Ok();
        }, ct);
    }
    public async Task<Envelope> ActiveProduct(int productId, CancellationToken ct = default) => await ToggleStatus(productId, true, ct: ct);
    public async Task<Envelope> DeactiveProduct(int productId, CancellationToken ct = default) => await ToggleStatus(productId, false, ct: ct);
    public async Task<Envelope> ToggleStatus(int productId, bool? state, CancellationToken ct = default)
    {
        return await uOw.ExecuteInTransactionAsync(async _ =>
        {
            if (productId <= 0)
                return Envelope.BadRequest().WithError("product id", "Invalid product id, need to be positive.");

            var product = await repository.GetByIdAsync(productId, asNoTracking: false, ct: ct);
            if (product is null) return Envelope.NotFound("Product not found.");

            product.ToggleStatus(state);
            return Envelope.Ok();
        }, ct);

    }
    public async Task<Envelope> UpdatePrice(ProductUpdatePrice updatePrice, CancellationToken ct = default)
    {
        return await uOw.ExecuteInTransactionAsync(async _ =>
        {
            var product = await repository.GetByIdAsync(updatePrice.Id, asNoTracking:false, ct: ct);
            if (product is null) 
                return Envelope.NotFound("Product not found.");
        
            product.UpdatePrice(updatePrice.Price, updatePrice.FormatCurrency);
            product.RowVersion = updatePrice.RowVersion; // TODO
            return Envelope.Ok();
        }, ct);
    }
    public async Task<Envelope> UpdateImageAsync(int id, string imageUrl, CancellationToken ct = default)
    {
        return await uOw.ExecuteInTransactionAsync(async _ =>
        {
            var product = await repository.GetByIdAsync(id, asNoTracking: false, ct: ct);
            if (product is null) return Envelope.NotFound("Product not found.");

            if (string.IsNullOrWhiteSpace(imageUrl))
                return Envelope.BadRequest().WithError(nameof(imageUrl), "Invalid image url.");
            
            //product.ImageUrl = imageUrl; TODO
            return Envelope.Ok();
        }, ct);

    }

    #endregion
}