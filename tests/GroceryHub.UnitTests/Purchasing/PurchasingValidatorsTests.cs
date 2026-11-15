using FluentValidation.TestHelper;
using GroceryHub.Modules.Purchasing.Application.DTOs;
using GroceryHub.Modules.Purchasing.Application.Validators;
using Xunit;

namespace GroceryHub.UnitTests.Purchasing;

public class PurchasingValidatorsTests
{
    private readonly CreateSupplierRequestValidator _createSupplierValidator = new();
    private readonly UpdateSupplierRequestValidator _updateSupplierValidator = new();
    private readonly CreatePurchaseOrderRequestValidator _createPoValidator = new();
    private readonly ReceivePurchaseOrderRequestValidator _receivePoValidator = new();

    [Fact]
    public void CreateSupplierRequest_Fails_WhenCodeOrNameIsEmpty()
    {
        var model = new CreateSupplierRequest("", "", null, "0901234567", null, null, null, null);
        var result = _createSupplierValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Code);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void CreateSupplierRequest_Fails_WhenPhoneInvalid()
    {
        var model = new CreateSupplierRequest("SUP-01", "Supplier 1", null, "12345", null, null, null, null);
        var result = _createSupplierValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public void CreateSupplierRequest_Passes_WhenValid()
    {
        var model = new CreateSupplierRequest("SUP-01", "Supplier 1", "Contact", "0901234567", "sup@test.com", "Address", "0102030405", "Note");
        var result = _createSupplierValidator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreatePurchaseOrderRequest_Fails_WhenNoItems()
    {
        var model = new CreatePurchaseOrderRequest(Guid.NewGuid(), null, []);
        var result = _createPoValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void CreatePurchaseOrderRequest_Fails_WhenItemQuantityZeroOrNegative()
    {
        var model = new CreatePurchaseOrderRequest(
            Guid.NewGuid(),
            null,
            [new PurchaseOrderItemRequest(Guid.NewGuid(), 0, 10000)]);

        var result = _createPoValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("Items[0].OrderedQuantity");
    }

    [Fact]
    public void CreatePurchaseOrderRequest_Passes_WhenValid()
    {
        var model = new CreatePurchaseOrderRequest(
            Guid.NewGuid(),
            "Initial order",
            [new PurchaseOrderItemRequest(Guid.NewGuid(), 50, 10000)]);

        var result = _createPoValidator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ReceivePurchaseOrderRequest_Fails_WhenEmptyItems()
    {
        var model = new ReceivePurchaseOrderRequest(null, null, []);
        var result = _receivePoValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void ReceivePurchaseOrderRequest_Fails_WhenReceivedQuantityZeroOrNegative()
    {
        var model = new ReceivePurchaseOrderRequest(
            null,
            null,
            [new ReceivePurchaseOrderItemRequest(null, Guid.NewGuid(), -1, 10000)]);

        var result = _receivePoValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("Items[0].ReceivedQuantity");
    }

    [Fact]
    public void ReceivePurchaseOrderRequest_Passes_WhenValid()
    {
        var model = new ReceivePurchaseOrderRequest(
            "GR-001",
            "Delivered batch 1",
            [new ReceivePurchaseOrderItemRequest(null, Guid.NewGuid(), 20, 10000)]);

        var result = _receivePoValidator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
