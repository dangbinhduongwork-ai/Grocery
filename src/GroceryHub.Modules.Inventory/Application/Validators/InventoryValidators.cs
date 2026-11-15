using FluentValidation;
using GroceryHub.Modules.Inventory.Application.DTOs;

namespace GroceryHub.Modules.Inventory.Application.Validators;

public sealed class AdjustStockRequestValidator : AbstractValidator<AdjustStockRequest>
{
    public AdjustStockRequestValidator()
    {
        RuleFor(static x => x.Delta)
            .NotEqual(0).WithMessage("Delta must not be zero.");

        RuleFor(static x => x.Reason)
            .NotEmpty().WithMessage("Reason is required.")
            .MaximumLength(255).WithMessage("Reason must not exceed 255 characters.");
    }
}

public sealed class AdminAdjustStockRequestValidator : AbstractValidator<AdminAdjustStockRequest>
{
    public AdminAdjustStockRequestValidator()
    {
        RuleFor(static x => x.ProductId)
            .NotEmpty().WithMessage("ProductId is required.");

        RuleFor(static x => x.Delta)
            .NotEqual(0).WithMessage("Delta must not be zero.");

        RuleFor(static x => x.Reason)
            .NotEmpty().WithMessage("Reason is required.")
            .MaximumLength(255).WithMessage("Reason must not exceed 255 characters.");
    }
}

public sealed class UpdateReorderLevelRequestValidator : AbstractValidator<UpdateReorderLevelRequest>
{
    public UpdateReorderLevelRequestValidator()
    {
        RuleFor(static x => x.ReorderLevel)
            .GreaterThanOrEqualTo(0).WithMessage("ReorderLevel cannot be negative.");
    }
}
