using FluentValidation.TestHelper;
using GroceryHub.Modules.Inventory.Application.DTOs;
using GroceryHub.Modules.Inventory.Application.Validators;
using GroceryHub.Modules.Inventory.Domain;
using Xunit;

namespace GroceryHub.UnitTests.Inventory;

public class InventoryInvariantsTests
{
    [Theory]
    [InlineData(10, 2, -5, true)]   // remaining 5 >= 2 reserved
    [InlineData(10, 2, -8, true)]   // remaining 2 == 2 reserved
    [InlineData(10, 2, -9, false)]  // remaining 1 < 2 reserved
    [InlineData(10, 0, -11, false)] // negative stock
    [InlineData(10, 2, 5, true)]    // stock increase
    [InlineData(0, 0, 10, true)]    // from 0 to 10
    [InlineData(0, 0, -1, false)]   // from 0 to -1
    [InlineData(5, 5, 0, true)]     // delta 0 doesn't violate invariant
    [InlineData(5, 5, -1, false)]   // violates reserved
    public void IsValidAdjustment_EvaluatesCorrectly(int onHand, int reserved, int delta, bool expected)
    {
        bool result = InventoryInvariants.IsValidAdjustment(onHand, reserved, delta);
        Assert.Equal(expected, result);
    }
}

public class InventoryValidatorsTests
{
    private readonly AdjustStockRequestValidator _adjustValidator = new();
    private readonly AdminAdjustStockRequestValidator _adminAdjustValidator = new();
    private readonly UpdateReorderLevelRequestValidator _reorderValidator = new();

    [Fact]
    public void AdjustStockRequest_Fails_WhenDeltaIsZero()
    {
        var model = new AdjustStockRequest(0, "Damage");
        var result = _adjustValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Delta);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AdjustStockRequest_Fails_WhenReasonIsEmpty(string? reason)
    {
        var model = new AdjustStockRequest(5, reason!);
        var result = _adjustValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void AdjustStockRequest_Passes_WhenValid()
    {
        var model = new AdjustStockRequest(-3, "Damage");
        var result = _adjustValidator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AdminAdjustStockRequest_Fails_WhenProductIdEmpty()
    {
        var model = new AdminAdjustStockRequest(Guid.Empty, 5, "CountCorrection");
        var result = _adminAdjustValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Fact]
    public void UpdateReorderLevelRequest_Fails_WhenNegative()
    {
        var model = new UpdateReorderLevelRequest(-1);
        var result = _reorderValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ReorderLevel);
    }

    [Fact]
    public void UpdateReorderLevelRequest_Passes_WhenZeroOrPositive()
    {
        var model0 = new UpdateReorderLevelRequest(0);
        var model10 = new UpdateReorderLevelRequest(10);
        _reorderValidator.TestValidate(model0).ShouldNotHaveAnyValidationErrors();
        _reorderValidator.TestValidate(model10).ShouldNotHaveAnyValidationErrors();
    }
}
