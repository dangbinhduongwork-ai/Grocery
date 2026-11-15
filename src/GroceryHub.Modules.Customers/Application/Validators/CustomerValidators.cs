using FluentValidation;
using GroceryHub.Modules.Customers.Application.DTOs;
using GroceryHub.SharedKernel;

namespace GroceryHub.Modules.Customers.Application.Validators;

public sealed class CreateAddressValidator : AbstractValidator<CreateAddressRequest>
{
    public CreateAddressValidator()
    {
        RuleFor(x => x.RecipientName)
            .NotEmpty().WithMessage("Recipient name is required.")
            .MaximumLength(100).WithMessage("Recipient name cannot exceed 100 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .Must(VietnamesePhone.IsValid)
            .WithMessage("Phone number must be a valid 10-digit Vietnamese mobile number starting with 03, 05, 07, 08, or 09.");

        RuleFor(x => x.ProvinceCode)
            .NotEmpty().WithMessage("Province code is required.")
            .MaximumLength(20).WithMessage("Province code cannot exceed 20 characters.");

        RuleFor(x => x.WardCode)
            .NotEmpty().WithMessage("Ward code is required.")
            .MaximumLength(20).WithMessage("Ward code cannot exceed 20 characters.");

        RuleFor(x => x.Street)
            .NotEmpty().WithMessage("Street address is required.")
            .MaximumLength(200).WithMessage("Street address cannot exceed 200 characters.");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Note cannot exceed 500 characters.");
    }
}

public sealed class UpdateAddressValidator : AbstractValidator<UpdateAddressRequest>
{
    public UpdateAddressValidator()
    {
        RuleFor(x => x.RecipientName)
            .NotEmpty().WithMessage("Recipient name is required.")
            .MaximumLength(100).WithMessage("Recipient name cannot exceed 100 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .Must(VietnamesePhone.IsValid)
            .WithMessage("Phone number must be a valid 10-digit Vietnamese mobile number starting with 03, 05, 07, 08, or 09.");

        RuleFor(x => x.ProvinceCode)
            .NotEmpty().WithMessage("Province code is required.")
            .MaximumLength(20).WithMessage("Province code cannot exceed 20 characters.");

        RuleFor(x => x.WardCode)
            .NotEmpty().WithMessage("Ward code is required.")
            .MaximumLength(20).WithMessage("Ward code cannot exceed 20 characters.");

        RuleFor(x => x.Street)
            .NotEmpty().WithMessage("Street address is required.")
            .MaximumLength(200).WithMessage("Street address cannot exceed 200 characters.");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Note cannot exceed 500 characters.");
    }
}

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.");

        RuleFor(x => x.Phone)
            .Must(p => string.IsNullOrWhiteSpace(p) || VietnamesePhone.IsValid(p))
            .WithMessage("Phone number must be a valid 10-digit Vietnamese mobile number starting with 03, 05, 07, 08, or 09.");
    }
}
