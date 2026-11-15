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
        // 9 characters (length < 10)
        var result = _validator.Validate("P@ss1234");

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_FAILED", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors!["Password"], msg => msg.Contains("at least 10 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_MissingUppercase_ReturnsFailure()
    {
        var result = _validator.Validate("p@ssword1234");

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_FAILED", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors!["Password"], msg => msg.Contains("uppercase", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_MissingLowercase_ReturnsFailure()
    {
        var result = _validator.Validate("P@SSWORD1234");

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_FAILED", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors!["Password"], msg => msg.Contains("lowercase", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_MissingDigit_ReturnsFailure()
    {
        var result = _validator.Validate("P@sswordLong!");

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_FAILED", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors!["Password"], msg => msg.Contains("digit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_MissingNonAlphanumeric_ReturnsFailure()
    {
        var result = _validator.Validate("Password1234");

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_FAILED", result.Error.Code);
        Assert.Contains(result.Error.FieldErrors!["Password"], msg => msg.Contains("non-alphanumeric", StringComparison.OrdinalIgnoreCase));
    }
}
