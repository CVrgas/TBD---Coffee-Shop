using Domain.Base.ValueObjects;
using Domain.Catalog;

namespace Unit.Catalog;

public class ProductUnitTests
{
            [Fact]
        public void Create_WithValidData_InstantiatesProductWithCorrectProperties()
        {
            // Arrange
            const string name = "Test Product";
            const string sku = "TP-001";
            const decimal price = 10.50m;
            const string currencyCode = "USD";
            const int categoryId = 1;
            const string description = "This is a test product.";
            const string imageUrl = "http://example.com/image.png";

            // Act
            var product = Product.Create(name, sku, price, currencyCode, categoryId, description, imageUrl);

            // Assert
            Assert.Equal(name, product.Name);
            Assert.Equal(sku, product.Sku);
            Assert.Equal(price, product.Price);
            Assert.Equal(currencyCode, product.Currency.Code);
            Assert.Equal(categoryId, product.CategoryId);
            Assert.Equal(description, product.Description);
            Assert.Equal(imageUrl, product.ImageUrl);
        }

        [Fact]
        public void Create_SetsIsActiveToTrueAndInitializesCreatedAt()
        {
            // Arrange
            var utcNow = DateTimeOffset.UtcNow;

            // Act
            var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);

            // Assert
            Assert.True(product.IsActive);
            Assert.True(product.CreatedAt - utcNow < TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Create_WithEmptyOrWhiteSpaceName_ThrowsArgumentException(string name)
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentException>(() => Product.Create(name, "TP-001", 10.50m, "USD", 1));
        }

        [Fact]
        public void Create_WithZeroPrice_ThrowsArgumentOutOfRangeException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create("Test Product", "TP-001", 0, "USD", 1));
        }

        [Fact]
        public void Create_WithNegativePrice_ThrowsArgumentOutOfRangeException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create("Test Product", "TP-001", -1, "USD", 1));
        }

        [Fact]
        public void UpdatePrice_WithValidPrice_UpdatesPriceAndCurrency()
        {
            // Arrange
            var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);
            var newPrice = 12.00m;
            var newCurrency = new CurrencyCode("EUR");

            // Act
            product.UpdatePrice(newPrice, newCurrency);

            // Assert
            Assert.Equal(newPrice, product.Price);
            Assert.Equal(newCurrency, product.Currency);
            Assert.NotNull(product.UpdatedAt);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void UpdatePrice_WithInvalidPrice_ThrowsArgumentOutOfRangeException(decimal newPrice)
        {
            // Arrange
            var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => product.UpdatePrice(newPrice, new CurrencyCode("USD")));
        }

        [Fact]
        public void AddRating_WithValidRating_UpdatesRatingProperties()
        {
            // Arrange
            var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);

            // Act
            product.AddRating(4);
            product.AddRating(5);

            // Assert
            Assert.Equal(2, product.RatingCount);
            Assert.Equal(9, product.RatingSum);
            Assert.Equal(4.5m, product.AverageRating);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        public void AddRating_WithInvalidRating_ThrowsArgumentOutOfRangeException(int rating)
        {
            // Arrange
            var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => product.AddRating(rating));
        }
        
        [Fact]
        public void AverageRating_WithNoRatings_ReturnsZero()
        {
            // Arrange
            var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);

            // Act & Assert
            Assert.Equal(0, product.AverageRating);
        }

        [Fact]
        public void AverageRating_WithMultipleRatings_CalculatesCorrectAverage()
        {
            var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);
            var ratings = new List<int>();
            for (var i = 0; i < 10; i++)
            {
                var rating = i % 5 + 1;
                ratings.Add(rating);
                product.AddRating(rating);
            }
            var expectedAverage = (decimal)ratings.Average();
            Assert.Equal(expectedAverage, product.AverageRating);
        }

        [Fact]
        public void ToggleStatus_WhenActive_DeactivatesProduct()
        {
            // Arrange
            var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);

            // Act
            product.ToggleStatus();

            // Assert
            Assert.False(product.IsActive);
            Assert.NotNull(product.UpdatedAt);
        }

        [Fact]
        public void ToggleStatus_WhenInactive_ActivatesProduct()
        {
            // Arrange
            var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);
            product.ToggleStatus(); // Deactivate

            // Act
            product.ToggleStatus(); // Activate

            // Assert
            Assert.True(product.IsActive);
        }
        
        [Fact]
        public void ToggleStatus_WithExplicitState_SetsCorrectState()
        {
            // Arrange
            var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);

            // Act
            product.ToggleStatus(false);

            // Assert
            Assert.False(product.IsActive);
        }
        
        [Fact]
        public void ToggleStatus_WithExplicitTrueWhenAlreadyTrue_RemainsTrue()
        {
            // Arrange
            var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);

            // Act
            product.ToggleStatus(true); // Explicitly set to true

            // Assert
            Assert.True(product.IsActive);
        }

        [Fact]
        public void UpdateDetails_WithValidData_UpdatesProductDetails()
        {
            // Arrange
            var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);
            var newName = "Updated Product";
            var newDescription = "Updated description.";
            var newCategoryId = 2;

            // Act
            product.UpdateDetails(newName, newDescription, newCategoryId);

            // Assert
            Assert.Equal(newName, product.Name);
            Assert.Equal(newDescription, product.Description);
            Assert.Equal(newCategoryId, product.CategoryId);
            Assert.NotNull(product.UpdatedAt);
        }
        
        [Fact]
        public void UpdateDetails_WithPartialUpdate_RetainsPreviousValues()
        {
            // Arrange
            const string initialName = "Test Product";
            const string initialDescription = "Initial description.";
            const int initialCategoryId = 1;
            var product = Product.Create(initialName, "TP-001", 10.50m, "USD", initialCategoryId, initialDescription);
            const string newName = "Updated Product Name";

            // Act
            product.UpdateDetails(newName, null, null);

            // Assert
            Assert.Equal(newName, product.Name);
            Assert.Equal(initialDescription, product.Description);
            Assert.Equal(initialCategoryId, product.CategoryId);
            Assert.NotNull(product.UpdatedAt);
        }

        [Fact]
        public void UpdateDetails_WithAllNullOrEmptyValues_ThrowsArgumentException()
        {
            // Arrange
            var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => product.UpdateDetails(null, null, null));
            Assert.Throws<ArgumentException>(() => product.UpdateDetails("", "", 0));
        }
        
        [Fact]
        public void UpdateDetails_WithEmptyName_DoesNotUpdateName()
        {
            // Arrange
            const string initialName = "Test Product";
            var product = Product.Create(initialName, "TP-001", 10.50m, "USD", 1);
            const string newName = "  ";

            // Act & Assert
            product.UpdateDetails(newName, null, 2);
            
            Assert.Equal(initialName, product.Name); // Name should remain unchanged
            Assert.Equal(2, product.CategoryId); // Category should update even if name is whitespace
        }
        
        [Fact]
        public void UpdateDetails_WithWhiteSpaceName_DoesNotUpdateNameOrThrows()
        {
            // Arrange
            const string initialName = "Test Product";
            var product = Product.Create(initialName, "TP-001", 10.50m, "USD", 1);
            const string newName = "   ";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => product.UpdateDetails(newName, null, null));
            Assert.Equal(initialName, product.Name); // Name should remain unchanged
        }

        [Fact]
        public void UpdateDetails_WithValidNameOnly_UpdatesNameLeavesRestUnchanged()
        {
            // Arrange
            const string initialName = "Test Product";
            const string initialDescription = "Initial description.";
            const int initialCategoryId = 1;
            var product = Product.Create(initialName, "TP-001", 10.50m, "USD", initialCategoryId, initialDescription);
            const string newName = "Updated Product Name";

            // Act
            product.UpdateDetails(newName, null, null);

            // Assert
            Assert.Equal(newName, product.Name);
            Assert.Equal(initialDescription, product.Description);
            Assert.Equal(initialCategoryId, product.CategoryId);
        }

        [Fact]
        public void PutOnSale_WithValidPrice_SetsIsOnSaleTrue()
        {
            // Arrange
            var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);
            var salePrice = 8.00m;

            // Act
            product.PutOnSale(salePrice);

            // Assert
            Assert.True(product.IsOnSale);
            Assert.Equal(salePrice, product.SalePrice);
        }

        [Fact]
        public void PutOnSale_WithSalePriceHigherThanRegularPrice_ThrowsException()
        {
            // Arrange
            var product = Product.Create("Test Product", "TP-001", 10.50m, "USD", 1);
            var invalidSalePrice = 12.00m;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => product.PutOnSale(invalidSalePrice));
        }
}