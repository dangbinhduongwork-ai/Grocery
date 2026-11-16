using GroceryHub.Modules.Identity.Application;
using Xunit;

namespace GroceryHub.UnitTests.Identity;

public class PasswordPolicyValidatorTests
{
    private readonly PasswordPolicyValidator _validator = new();

    [Theory]
    [InlineData("Passw0rd!123")]
    [InlineData("Str0ng!P@ssw0rd")]
    [InlineData("Groc3ryHub#Secure2026")]
    [InlineData("password123")]
    [InlineData("12345678")]
    public void Validate_ValidPassword_ReturnsSuccess(string password)
    {
        var result = _validator.Validate(password);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_NullOrEmpty_ReturnsFailure(string? password)
    {
        var result = _validator.Validate(password!);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_FAILED", result.Error.Code);
        Assert.NotNull(result.Error.FieldErrors);
        Assert.True(result.Error.FieldErrors.ContainsKey("Password"));
    }

    [Fact]
    public void Validate_TooShort_ReturnsFailure()
    {
        // 5 characters (length < 8)
        var result = _validator.Validate("12345");

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_FAILED", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors!["Password"], msg => msg.Contains("8 ký tự", StringComparison.OrdinalIgnoreCase));
    }
}
