using FluentValidation;
using GroceryHub.Modules.Purchasing.Application.DTOs;
using GroceryHub.SharedKernel;

namespace GroceryHub.Modules.Purchasing.Application.Validators;

public sealed class CreateSupplierRequestValidator : AbstractValidator<CreateSupplierRequest>
{
    public CreateSupplierRequestValidator()
    {
        RuleFor(static x => x.Code)
            .NotEmpty().WithMessage("Supplier code is required.")
            .MaximumLength(50).WithMessage("Supplier code must not exceed 50 characters.");

        RuleFor(static x => x.Name)
            .NotEmpty().WithMessage("Supplier name is required.")
            .MaximumLength(200).WithMessage("Supplier name must not exceed 200 characters.");

        RuleFor(static x => x.Phone)
            .NotEmpty().WithMessage("Supplier phone is required.")
            .Must(static p => VietnamesePhone.IsValid(p)).WithMessage("Invalid Vietnamese phone number format.");

        RuleFor(static x => x.Email)
            .EmailAddress().When(static x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Invalid email format.")
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters.");

        RuleFor(static x => x.ContactPerson)
            .MaximumLength(100).WithMessage("Contact person must not exceed 100 characters.");

        RuleFor(static x => x.TaxCode)
            .MaximumLength(50).WithMessage("Tax code must not exceed 50 characters.");
    }
}

public sealed class UpdateSupplierRequestValidator : AbstractValidator<UpdateSupplierRequest>
{
    public UpdateSupplierRequestValidator()
    {
        RuleFor(static x => x.Name)
            .NotEmpty().WithMessage("Supplier name is required.")
            .MaximumLength(200).WithMessage("Supplier name must not exceed 200 characters.");

        RuleFor(static x => x.Phone)
            .NotEmpty().WithMessage("Supplier phone is required.")
            .Must(static p => VietnamesePhone.IsValid(p)).WithMessage("Invalid Vietnamese phone number format.");

        RuleFor(static x => x.Email)
            .EmailAddress().When(static x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Invalid email format.")
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters.");

        RuleFor(static x => x.ContactPerson)
            .MaximumLength(100).WithMessage("Contact person must not exceed 100 characters.");

        RuleFor(static x => x.TaxCode)
            .MaximumLength(50).WithMessage("Tax code must not exceed 50 characters.");
    }
}

public sealed class PurchaseOrderItemRequestValidator : AbstractValidator<PurchaseOrderItemRequest>
{
    public PurchaseOrderItemRequestValidator()
    {
        RuleFor(static x => x.ProductId)
            .NotEmpty().WithMessage("ProductId is required.");

        RuleFor(static x => x.OrderedQuantity)
            .GreaterThan(0).WithMessage("OrderedQuantity must be greater than zero.");

        RuleFor(static x => x.UnitCost)
            .GreaterThanOrEqualTo(0).WithMessage("UnitCost must be greater than or equal to zero.");
    }
}

public sealed class CreatePurchaseOrderRequestValidator : AbstractValidator<CreatePurchaseOrderRequest>
{
    public CreatePurchaseOrderRequestValidator()
    {
        RuleFor(static x => x.SupplierId)
            .NotEmpty().WithMessage("SupplierId is required.");

        RuleFor(static x => x.Items)
            .NotEmpty().WithMessage("Purchase order must contain at least one line item.");

        RuleForEach(static x => x.Items)
            .SetValidator(new PurchaseOrderItemRequestValidator());
    }
}

public sealed class UpdatePurchaseOrderRequestValidator : AbstractValidator<UpdatePurchaseOrderRequest>
{
    public UpdatePurchaseOrderRequestValidator()
    {
        RuleFor(static x => x.Items)
            .NotEmpty().WithMessage("Purchase order must contain at least one line item.");

        RuleForEach(static x => x.Items)
            .SetValidator(new PurchaseOrderItemRequestValidator());
    }
}

public sealed class ReceivePurchaseOrderItemRequestValidator : AbstractValidator<ReceivePurchaseOrderItemRequest>
{
    public ReceivePurchaseOrderItemRequestValidator()
    {
        RuleFor(static x => x.ProductId)
            .NotEmpty().WithMessage("ProductId is required.");

        RuleFor(static x => x.ReceivedQuantity)
            .GreaterThan(0).WithMessage("ReceivedQuantity must be greater than zero.");

        RuleFor(static x => x.UnitCost)
            .GreaterThanOrEqualTo(0).WithMessage("UnitCost must be greater than or equal to zero.");
    }
}

public sealed class ReceivePurchaseOrderRequestValidator : AbstractValidator<ReceivePurchaseOrderRequest>
{
    public ReceivePurchaseOrderRequestValidator()
    {
        RuleFor(static x => x.Items)
            .NotEmpty().WithMessage("At least one item must be received.");

        RuleForEach(static x => x.Items)
            .SetValidator(new ReceivePurchaseOrderItemRequestValidator());
    }
}

public sealed class CreateGoodsReceiptLineRequestValidator : AbstractValidator<CreateGoodsReceiptLineRequest>
{
    public CreateGoodsReceiptLineRequestValidator()
    {
        RuleFor(static x => x.ProductId)
            .NotEmpty().WithMessage("ProductId is required.");

        RuleFor(static x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

        RuleFor(static x => x.UnitCost)
            .GreaterThanOrEqualTo(0).WithMessage("UnitCost must be greater than or equal to zero.");
    }
}

public sealed class CreateGoodsReceiptRequestValidator : AbstractValidator<CreateGoodsReceiptRequest>
{
    public CreateGoodsReceiptRequestValidator()
    {
        RuleFor(static x => x.SupplierId)
            .NotEmpty().WithMessage("SupplierId is required.");

        RuleFor(static x => x.Lines)
            .NotEmpty().WithMessage("Goods receipt must contain at least one line item.");

        RuleForEach(static x => x.Lines)
            .SetValidator(new CreateGoodsReceiptLineRequestValidator());
    }
}
