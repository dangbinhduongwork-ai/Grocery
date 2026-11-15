using FluentValidation;
using GroceryHub.Modules.Ordering.Application.DTOs;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;

namespace GroceryHub.Modules.Ordering.Application.Validators;

public sealed class ShippingQuoteValidator : AbstractValidator<ShippingQuoteRequest>
{
    public ShippingQuoteValidator()
    {
        RuleFor(x => x.Subtotal)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Subtotal must be greater than or equal to zero.");

        RuleFor(x => x)
            .Must(x => x.AddressId.HasValue || !string.IsNullOrWhiteSpace(x.ProvinceCode))
            .WithMessage("Either AddressId or ProvinceCode must be provided for a shipping quote.");
    }
}

public sealed class CheckoutQuoteValidator : AbstractValidator<CheckoutQuoteRequest>
{
    public CheckoutQuoteValidator()
    {
        RuleFor(x => x.AddressId)
            .NotEmpty()
            .WithMessage("AddressId is required for checkout quote.");
    }
}

public sealed class PlaceOrderValidator : AbstractValidator<PlaceOrderRequest>
{
    public PlaceOrderValidator()
    {
        RuleFor(x => x.AddressId)
            .NotEmpty()
            .WithMessage("Delivery address is required.");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .WithMessage("Payment method is required.")
            .Must(m => OrderPaymentMethodConstants.All.Contains(m))
            .WithMessage($"Payment method must be one of: {string.Join(", ", OrderPaymentMethodConstants.All)}.");

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .WithMessage("Customer note cannot exceed 500 characters.");
    }
}

public sealed class CancelOrderValidator : AbstractValidator<CancelOrderRequest>
{
    public CancelOrderValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Cancellation reason is required.")
            .MaximumLength(500)
            .WithMessage("Cancellation reason cannot exceed 500 characters.");
    }
}

public sealed class TransitionOrderValidator : AbstractValidator<TransitionOrderRequest>
{
    public TransitionOrderValidator()
    {
        RuleFor(x => x.To)
            .NotEmpty()
            .WithMessage("Target order status 'To' is required.")
            .Must(s => OrderStatusConstants.All.Contains(s))
            .WithMessage($"Target status must be one of: {string.Join(", ", OrderStatusConstants.All)}.");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("Transition reason cannot exceed 500 characters.");
    }
}

public sealed class OrderNoteValidator : AbstractValidator<OrderNoteRequest>
{
    public OrderNoteValidator()
    {
        RuleFor(x => x.InternalNote)
            .NotEmpty()
            .WithMessage("Internal note cannot be empty.")
            .MaximumLength(1000)
            .WithMessage("Internal note cannot exceed 1000 characters.");
    }
}

public sealed class CreateShippingZoneValidator : AbstractValidator<CreateShippingZoneRequest>
{
    public CreateShippingZoneValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Zone name is required.")
            .MaximumLength(100).WithMessage("Zone name cannot exceed 100 characters.");

        RuleFor(x => x.ProvinceCodes)
            .NotEmpty().WithMessage("At least one province code must be mapped to this zone.");

        RuleFor(x => x.Rates)
            .NotEmpty().WithMessage("At least one shipping rate must be configured.");
    }
}

public sealed class UpdateShippingZoneValidator : AbstractValidator<UpdateShippingZoneRequest>
{
    public UpdateShippingZoneValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Zone name is required.")
            .MaximumLength(100).WithMessage("Zone name cannot exceed 100 characters.");

        RuleFor(x => x.ProvinceCodes)
            .NotEmpty().WithMessage("At least one province code must be mapped to this zone.");

        RuleFor(x => x.Rates)
            .NotEmpty().WithMessage("At least one shipping rate must be configured.");
    }
}

public sealed class StoreSettingsValidator : AbstractValidator<StoreSettingsDto>
{
    public StoreSettingsValidator()
    {
        RuleFor(x => x.StoreName)
            .NotEmpty().WithMessage("Store name is required.")
            .MaximumLength(100).WithMessage("Store name cannot exceed 100 characters.");

        RuleFor(x => x.SupportEmail)
            .NotEmpty().WithMessage("Support email is required.")
            .EmailAddress().WithMessage("Support email is invalid.");

        RuleFor(x => x.SupportPhone)
            .NotEmpty().WithMessage("Support phone is required.")
            .Must(VietnamesePhone.IsValid).WithMessage("Support phone must be a valid Vietnamese phone number.");

        RuleFor(x => x.FreeShippingThreshold)
            .GreaterThanOrEqualTo(0).WithMessage("Free shipping threshold must be greater than or equal to 0.");

        RuleFor(x => x.CodMaxAmount)
            .GreaterThanOrEqualTo(0).WithMessage("COD maximum amount must be greater than or equal to 0.");

        RuleFor(x => x.MaxQuantityPerLine)
            .InclusiveBetween(1, 99).WithMessage("Max quantity per line must be between 1 and 99.");
    }
}
