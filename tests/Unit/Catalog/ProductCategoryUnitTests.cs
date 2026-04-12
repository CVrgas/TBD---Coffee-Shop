using Domain.Catalog;

namespace Unit.Catalog;

public class ProductCategoryUnitTests
{
    [Fact]
        public void ProductCategory_Create_WithValidData_InstantiatesCategoryWithCorrectProperties()
        {
            // Arrange
            const string name = "Test Category";
            const string slug = "test-category";
            const string code = "TC001";
            const string description = "This is a test category.";
            const int parentId = 1;

            // Act
            var category = ProductCategory.Create(name, slug, code, description, parentId);

            // Assert
            Assert.Equal(name, category.Name);
            Assert.Equal(slug, category.Slug);
            Assert.Equal(code, category.Code);
            Assert.Equal(description, category.Description);
            Assert.Equal(parentId, category.ParentId);
        }

        [Fact]
        public void ProductCategory_Create_SetsIsActiveToTrueAndInitializesCreatedAt()
        {
            // Arrange
            var utcNow = DateTime.UtcNow;

            // Act
            var category = ProductCategory.Create("Test Category", "test-category", "TC001");

            // Assert
            Assert.True(category.IsActive);
            Assert.True(category.CreatedAt >= utcNow);
        }

        [Fact]
        public void ProductCategory_Create_WithCodeContainingSpaces_FormatsCodeToUpperCaseAndRemovesSpaces()
        {
            // Arrange
            const string codeWithSpaces = "tc 001";
            const string expectedCode = "TC001";

            // Act
            var category = ProductCategory.Create("Test Category", "test-category", codeWithSpaces);

            // Assert
            Assert.Equal(expectedCode, category.Code);
        }

        [Fact]
        public void ProductCategory_Create_WithLowerCaseCode_FormatsCodeToUpperCase()
        {
            // Arrange
            const string lowerCaseCode = "tc001";
            const string expectedCode = "TC001";

            // Act
            var category = ProductCategory.Create("Test Category", "test-category", lowerCaseCode);

            // Assert
            Assert.Equal(expectedCode, category.Code);
        }

        [Fact]
        public void ProductCategory_Create_WithNullDescription_AllowsNullDescription()
        {
            // Act
            var category = ProductCategory.Create("Test Category", "test-category", "TC001", null);

            // Assert
            Assert.Null(category.Description);
        }

        [Fact]
        public void ProductCategory_Create_WithNullParentId_AllowsNullParentId()
        {
            // Act
            var category = ProductCategory.Create("Test Category", "test-category", "TC001", null, null);

            // Assert
            Assert.Null(category.ParentId);
        }
}