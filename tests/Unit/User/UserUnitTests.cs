using Domain.Users.Enum;
using Domain.Users.ValueObjects;
using EmailAddress = Domain.Users.ValueObjects.EmailAddress;

namespace Unit.User;

public class UserUnitTests
{
    // --- EmailAddress Tests ---
    
    [Theory]
    [InlineData("test@example.com")]
    [InlineData(" user@domain.co.uk ")]
    [InlineData("UPPERCASE@DOMAIN.COM")]
    public void EmailAddress_WithValidFormat_CreatesInstance(string email)
    {
        var emailAddress = new EmailAddress(email);
        Assert.Equal(email.Trim().ToLowerInvariant(), emailAddress.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void EmailAddress_WithEmptyOrNull_ThrowsArgumentNullException(string email)
    {
        Assert.Throws<ArgumentNullException>(() => new EmailAddress(email));
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("test@")]
    [InlineData("@domain.com")]
    [InlineData("test@domain")]
    public void EmailAddress_WithInvalidFormat_ThrowsArgumentException(string email)
    {
        Assert.Throws<ArgumentException>(() => new EmailAddress(email));
    }

    // --- PasswordHash Tests ---
    
    [Fact]
    public void PasswordHash_WithValidString_CreatesInstance()
    {
        var hash = new PasswordHash("hashed_string");
        Assert.Equal("hashed_string", hash.Value);
        Assert.Equal("hashed_string", (string)hash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void PasswordHash_WithEmptyOrNull_ThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => new PasswordHash(value));
    }

    // --- User Tests ---
    
    [Fact]
    public void CreateCustomer_WithValidData_InstantiatesCustomerCorrectly()
    {
        var hash = new PasswordHash("hash123");
        var user = Domain.Users.Entities.User.CreateCustomer("John", "Doe", "john@example.com", hash);

        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal("john@example.com", user.Email.Value);
        Assert.Equal(UserRole.Customer, user.Role);
        Assert.True(user.IsActive);
        Assert.NotNull(user.SecurityStamp);
        Assert.Equal(hash, user.PasswordHash);
    }

    [Theory]
    [InlineData("", "Doe")]
    [InlineData("John", " ")]
    [InlineData(null, "Doe")]
    public void CreateCustomer_WithInvalidNames_ThrowsArgumentNullException(string firstName, string lastName)
    {
        var hash = new PasswordHash("hash123");
        Assert.Throws<ArgumentNullException>(() => Domain.Users.Entities.User.CreateCustomer(firstName, lastName, "john@example.com", hash));
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Staff)]
    public void CreateStaff_WithValidData_InstantiatesStaffCorrectly(UserRole role)
    {
        var hash = new PasswordHash("hash123");
        var user = Domain.Users.Entities.User.CreateStaff("Jane", "Smith", "jane@example.com", role, hash);

        Assert.Equal("Jane", user.FirstName);
        Assert.Equal("Smith", user.LastName);
        Assert.Equal("jane@example.com", user.Email.Value);
        Assert.Equal(role, user.Role);
        Assert.True(user.IsActive);
    }

    [Theory]
    [InlineData(UserRole.Customer)]
    [InlineData(UserRole.Anonymous)]
    public void CreateStaff_WithInvalidRole_ThrowsArgumentException(UserRole role)
    {
        var hash = new PasswordHash("hash123");
        Assert.Throws<ArgumentException>(() => Domain.Users.Entities.User.CreateStaff("Jane", "Smith", "jane@example.com", role, hash));
    }

    [Fact]
    public void UpdateProfile_WithValidData_UpdatesNamesAndSetsUpdatedAt()
    {
        var user = Domain.Users.Entities.User.CreateCustomer("John", "Doe", "john@example.com", new PasswordHash("hash123"));
        
        user.UpdateProfile("Johnny", "D");

        Assert.Equal("Johnny", user.FirstName);
        Assert.Equal("D", user.LastName);
        Assert.NotNull(user.UpdatedAt);
    }

    [Theory]
    [InlineData("", "Doe")]
    [InlineData("John", " ")]
    public void UpdateProfile_WithInvalidNames_ThrowsArgumentNullException(string firstName, string lastName)
    {
        var user = Domain.Users.Entities.User.CreateCustomer("John", "Doe", "john@example.com", new PasswordHash("hash123"));
        Assert.Throws<ArgumentNullException>(() => user.UpdateProfile(firstName, lastName));
    }

    [Fact]
    public void UpdatePassword_WithNewHash_UpdatesHashAndRegeneratesSecurityStamp()
    {
        var oldHash = new PasswordHash("oldHash");
        var newHash = new PasswordHash("newHash");
        var user = Domain.Users.Entities.User.CreateCustomer("John", "Doe", "john@example.com", oldHash);
        var oldStamp = user.SecurityStamp;

        user.UpdatePassword(newHash);

        Assert.Equal(newHash, user.PasswordHash);
        Assert.NotEqual(oldStamp, user.SecurityStamp);
        Assert.NotNull(user.UpdatedAt);
    }

    [Fact]
    public void UpdatePassword_WithSameHash_ThrowsArgumentException()
    {
        var hash = new PasswordHash("sameHash");
        var user = Domain.Users.Entities.User.CreateCustomer("John", "Doe", "john@example.com", hash);

        Assert.Throws<ArgumentException>(() => user.UpdatePassword(new PasswordHash("sameHash")));
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalseAndRegeneratesSecurityStamp()
    {
        var user = Domain.Users.Entities.User.CreateCustomer("John", "Doe", "john@example.com", new PasswordHash("hash123"));
        var oldStamp = user.SecurityStamp;

        user.Deactivate();

        Assert.False(user.IsActive);
        Assert.NotEqual(oldStamp, user.SecurityStamp);
        Assert.NotNull(user.UpdatedAt);
    }

    [Fact]
    public void Activate_SetsIsActiveToTrueAndSetsUpdatedAt()
    {
        var user = Domain.Users.Entities.User.CreateCustomer("John", "Doe", "john@example.com", new PasswordHash("hash123"));
        user.Deactivate();

        user.Activate();

        Assert.True(user.IsActive);
        Assert.NotNull(user.UpdatedAt);
    }
}