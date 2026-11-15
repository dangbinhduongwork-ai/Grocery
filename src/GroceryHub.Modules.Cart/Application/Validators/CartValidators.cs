using FluentValidation;
using GroceryHub.SharedKernel;

namespace GroceryHub.Modules.Cart.Application.Validators;

public sealed class AddCartItemValidator : AbstractValidator<AddCartItemRequest>
{
    public AddCartItemValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId is required.");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(1, 20)
            .WithMessage("Quantity must be between 1 and 20 units per line.");
    }
}

public sealed class UpdateCartItemValidator : AbstractValidator<UpdateCartItemRequest>
{
    public UpdateCartItemValidator()
    {
        RuleFor(x => x.Quantity)
            .InclusiveBetween(0, 20)
            .WithMessage("Quantity must be between 0 and 20 (0 removes the item).");
    }
}

public sealed class ApplyCouponValidator : AbstractValidator<ApplyCouponRequest>
{
    public ApplyCouponValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Coupon code is required.")
            .MaximumLength(50).WithMessage("Coupon code cannot exceed 50 characters.");
    }
}
