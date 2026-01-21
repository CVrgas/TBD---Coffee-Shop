using System.Linq.Expressions;
using Application.Catalog.Dtos;
using Application.Catalog.Interfaces;
using Application.Catalog.Mapping;
using Application.Common;
using Application.Common.Abstractions.Envelope;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Persistence.Repository;
using Domain.Base;
using Domain.Catalog;
using Domain.Inventory;
using Polly;
using Polly.Registry;

namespace Application.Catalog.Services;

public sealed class ProductService(
    IRepository<Product, int> repository, 
    IRepository<ProductCategory, int> categoryRepo,
    IUnitOfWork uOw, 
    ResiliencePipelineProvider<string> pipeline) : IProductService
{
    private readonly ResiliencePipeline _pipeline = pipeline.GetPipeline("default-retry-pipeline");

    #region Add Product
    
    public async Task<Envelope<ProductDto>> AddAsync(ProductCreateDto product, CancellationToken ct = default)
    {
        return await uOw.ExecuteInTransactionAsync(async (_) =>
        {
            var exist = await repository.ExistsAsync(p => p.Name == product.Name, ct: ct);
            if (exist)
                return Envelope<ProductDto>.BadRequest()
                    .WithError(nameof(product.Name), "Product name already exists");

            var category = await categoryRepo.GetByIdAsync(
                id: product.CategoryId,
                selector: pc => new { pc.Code, pc.Name },
                ct: ct);

            if (category is null)
                return Envelope<ProductDto>.BadRequest("Category does not exist")
                    .WithError(nameof(product.CategoryId), "Category does not exist");

            var entity = new Product
            {
                Name = product.Name,
                Sku = $"{Utilities.GenerateSku(category.Code)}",
                Price = product.Price,
                Currency = new CurrencyCode(product.Currency),
                Description = product.Description,
                ImageUrl = product.ImageUrl,
                CategoryId = product.CategoryId,
                StockItems = new List<StockItem> { new() { IsActive = true } }
            };

            await repository.Create(entity);
            return Envelope<ProductDto>.Ok(entity.ToDto() with { CategoryName = category.Name });
        }, ct);
    }
    
    public async Task<Envelope> BulkCreateAsync(List<ProductCreateDto> dtos, CancellationToken ct = default)
    {
        return await uOw.ExecuteInTransactionAsync(async _ =>
        {
            var incomingIds = dtos.Select(p => p.CategoryId).ToHashSet();
            var existingCategories = 
                (await categoryRepo.ListAsync(
                    selector: c => new {c.Id, c.Code}, 
                    predicate: pc => incomingIds.Contains(pc.Id), 
                    ct: ct)).ToDictionary(c => c.Id, c => c.Code);
            
            var missingIds = incomingIds.Where(id => !existingCategories.ContainsKey(id)).ToList();
            if (missingIds.Count != 0) 
                return Envelope.BadRequest()
                    .WithError("Categories", $"Missing categories: {string.Join(", ", missingIds)}");
            
            var products = new List<Product>();

            foreach (var dto in dtos)
            {
                var sku = Utilities.GenerateSku(existingCategories[dto.CategoryId]);
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
                var exist = await repository.ExistsAsync(p => 
                    p.Id != dto.ProductId && dto.Name != null && p.Name == dto.Name, ct: ct);
                if(exist) return Envelope<ProductDto>.BadRequest().WithError(nameof(product.Name),"Name already exists.");
            }
            
            if (dto.CategoryId.HasValue)
            {
                var categoryExist = await categoryRepo.ExistsAsync(c => c.Id == dto.CategoryId, ct: ct);
                if(!categoryExist) return Envelope<ProductDto>.NotFound("Category does not exist.");
            }
            
            if(dto.Name is not null) product.Name = dto.Name;
            if(dto.Description is not null) product.Description = dto.Description;
            if(dto.CategoryId is not null) product.CategoryId = (int)dto.CategoryId;
            if(dto.Name is not null || dto.Description is not null || dto.CategoryId is not null) product.UpdatedAt = DateTimeOffset.UtcNow;
            
            return Envelope<ProductDto>.Ok(product.ToDto());
        }, ct: ct);
    }
    public async Task<Envelope> RateProductAsync(int productId, int rate, CancellationToken ct = default)
    {
        return await uOw.ExecuteInTransactionAsync(async _ =>
        {
            var product = await repository.GetByIdAsync(productId, asNoTracking: false, ct: ct);
            if (product is null) return Envelope.NotFound("Product not found.");

            product.RatingSum += rate;
            product.RatingCount++;
            
            return Envelope.Ok();
        }, ct);
    }
    public async Task<Envelope> ActiveProduct(int productId, CancellationToken ct = default) => await ToggleStatus(productId, true, ct: ct);
    public async Task<Envelope> DeactiveProduct(int productId, CancellationToken ct = default) => await ToggleStatus(productId, false, ct: ct);
    public async Task<Envelope> ToggleStatus(int productId, bool? state, CancellationToken ct = default)
    {
        return await uOw.ExecuteInTransactionAsync(async _ =>
        {
            if (productId < 0)
                return Envelope.BadRequest().WithError("product id", "Invalid product id, need to be positive.");

            var product = await repository.GetByIdAsync(productId, asNoTracking: false, ct: ct);
            if (product is null) return Envelope.NotFound("Product not found.");

            product.IsActive = state ?? !product.IsActive;
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
            
            if(updatePrice.Price <= 0)
                return Envelope.BadRequest().WithError(nameof(updatePrice.Price), "Price must be positive.");
        
            product.Price = updatePrice.Price;
            product.Currency = updatePrice.FormatCurrency;
            product.RowVersion = updatePrice.RowVersion;
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

            product.ImageUrl = imageUrl;
            return Envelope.Ok();
        }, ct);

    }

    #endregion
}