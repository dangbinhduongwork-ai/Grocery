using FluentValidation;
using GroceryHub.Modules.Promotions.Application.DTOs;
using GroceryHub.Persistence.Entities;

namespace GroceryHub.Modules.Promotions.Application.Validators;

public sealed class CreateCouponValidator : AbstractValidator<CreateCouponRequest>
{
    public CreateCouponValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Coupon code is required.")
            .MaximumLength(50).WithMessage("Coupon code cannot exceed 50 characters.")
            .Matches(@"^[A-Za-z0-9_-]+$").WithMessage("Coupon code must contain only letters, numbers, hyphens, or underscores.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Coupon name is required.")
            .MaximumLength(100).WithMessage("Coupon name cannot exceed 100 characters.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Coupon type is required.")
            .Must(t => CouponTypeConstants.All.Contains(t))
            .WithMessage($"Coupon type must be one of: {string.Join(", ", CouponTypeConstants.All)}.");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("Coupon value must be greater than zero.");

        RuleFor(x => x.MinSubtotal)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum subtotal must be greater than or equal to zero.");

        RuleFor(x => x.EndsAt)
            .GreaterThan(x => x.StartsAt)
            .WithMessage("EndsAt must be later than StartsAt.");

        RuleFor(x => x.UsageLimit)
            .GreaterThan(0).When(x => x.UsageLimit.HasValue)
            .WithMessage("Usage limit must be greater than zero when specified.");

        RuleFor(x => x.PerUserLimit)
            .GreaterThan(0).When(x => x.PerUserLimit.HasValue)
            .WithMessage("Per-user limit must be greater than zero when specified.");

        RuleForEach(x => x.Scopes)
            .Must(s => CouponScopeTypes.All.Contains(s.ScopeType))
            .WithMessage($"Scope type must be one of: {string.Join(", ", CouponScopeTypes.All)}.");
    }
}

public sealed class UpdateCouponValidator : AbstractValidator<UpdateCouponRequest>
{
    public UpdateCouponValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Coupon code is required.")
            .MaximumLength(50).WithMessage("Coupon code cannot exceed 50 characters.")
            .Matches(@"^[A-Za-z0-9_-]+$").WithMessage("Coupon code must contain only letters, numbers, hyphens, or underscores.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Coupon name is required.")
            .MaximumLength(100).WithMessage("Coupon name cannot exceed 100 characters.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Coupon type is required.")
            .Must(t => CouponTypeConstants.All.Contains(t))
            .WithMessage($"Coupon type must be one of: {string.Join(", ", CouponTypeConstants.All)}.");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("Coupon value must be greater than zero.");

        RuleFor(x => x.MinSubtotal)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum subtotal must be greater than or equal to zero.");

        RuleFor(x => x.EndsAt)
            .GreaterThan(x => x.StartsAt)
            .WithMessage("EndsAt must be later than StartsAt.");

        RuleFor(x => x.UsageLimit)
            .GreaterThan(0).When(x => x.UsageLimit.HasValue)
            .WithMessage("Usage limit must be greater than zero when specified.");

        RuleFor(x => x.PerUserLimit)
            .GreaterThan(0).When(x => x.PerUserLimit.HasValue)
            .WithMessage("Per-user limit must be greater than zero when specified.");

        RuleForEach(x => x.Scopes)
            .Must(s => CouponScopeTypes.All.Contains(s.ScopeType))
            .WithMessage($"Scope type must be one of: {string.Join(", ", CouponScopeTypes.All)}.");
    }
}
