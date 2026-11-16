using FluentValidation;
using GroceryHub.Modules.Catalog.Application.DTOs;

namespace GroceryHub.Modules.Catalog.Application.Validators;

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(static x => x.Sku)
            .NotEmpty().WithMessage("SKU cannot be empty.")
            .MaximumLength(40).WithMessage("SKU cannot exceed 40 characters.");

        RuleFor(static x => x.Name)
            .NotEmpty().WithMessage("Product name cannot be empty.")
            .MaximumLength(250).WithMessage("Product name cannot exceed 250 characters.");

        RuleFor(static x => x.CategoryId)
            .NotEmpty().WithMessage("Category ID must be provided.");

        RuleFor(static x => x.Unit)
            .NotEmpty().WithMessage("Unit cannot be empty.")
            .MaximumLength(20).WithMessage("Unit cannot exceed 20 characters.");

        RuleFor(static x => x.ListPrice)
            .GreaterThanOrEqualTo(0).WithMessage("List price cannot be negative.");

        RuleFor(static x => x.SellingPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Selling price cannot be negative.")
            .LessThanOrEqualTo(static x => x.ListPrice).WithMessage("Selling price must be less than or equal to list price (BR-01).");

        RuleFor(static x => x.InitialCostPrice)
            .GreaterThanOrEqualTo(0).When(static x => x.InitialCostPrice.HasValue)
            .WithMessage("Initial cost price cannot be negative.");
    }
}

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(static x => x.Sku)
            .NotEmpty().WithMessage("SKU cannot be empty.")
            .MaximumLength(40).WithMessage("SKU cannot exceed 40 characters.");

        RuleFor(static x => x.Name)
            .NotEmpty().WithMessage("Product name cannot be empty.")
            .MaximumLength(250).WithMessage("Product name cannot exceed 250 characters.");

        RuleFor(static x => x.Slug)
            .NotEmpty().WithMessage("Slug cannot be empty.")
            .MaximumLength(280).WithMessage("Slug cannot exceed 280 characters.");

        RuleFor(static x => x.CategoryId)
            .NotEmpty().WithMessage("Category ID must be provided.");

        RuleFor(static x => x.Unit)
            .NotEmpty().WithMessage("Unit cannot be empty.")
            .MaximumLength(20).WithMessage("Unit cannot exceed 20 characters.");
    }
}

public sealed class UpdateProductPriceRequestValidator : AbstractValidator<UpdateProductPriceRequest>
{
    public UpdateProductPriceRequestValidator()
    {
        RuleFor(static x => x.ListPrice)
            .GreaterThanOrEqualTo(0).WithMessage("List price cannot be negative.");

        RuleFor(static x => x.SellingPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Selling price cannot be negative.")
            .LessThanOrEqualTo(static x => x.ListPrice).WithMessage("Selling price must be less than or equal to list price (BR-01).");

        RuleFor(static x => x.Reason)
            .NotEmpty().WithMessage("Reason for price update is required.");
    }
}

public sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(static x => x.Name)
            .NotEmpty().WithMessage("Category name cannot be empty.")
            .MaximumLength(120).WithMessage("Category name cannot exceed 120 characters.");
    }
}

public sealed class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(static x => x.Name)
            .NotEmpty().WithMessage("Category name cannot be empty.")
            .MaximumLength(120).WithMessage("Category name cannot exceed 120 characters.");

        RuleFor(static x => x.Slug)
            .NotEmpty().WithMessage("Slug cannot be empty.")
            .MaximumLength(150).WithMessage("Slug cannot exceed 150 characters.");
    }
}

public sealed class CreateBrandRequestValidator : AbstractValidator<CreateBrandRequest>
{
    public CreateBrandRequestValidator()
    {
        RuleFor(static x => x.Name)
            .NotEmpty().WithMessage("Brand name cannot be empty.")
            .MaximumLength(120).WithMessage("Brand name cannot exceed 120 characters.");
    }
}

public sealed class UpdateBrandRequestValidator : AbstractValidator<UpdateBrandRequest>
{
    public UpdateBrandRequestValidator()
    {
        RuleFor(static x => x.Name)
            .NotEmpty().WithMessage("Brand name cannot be empty.")
            .MaximumLength(120).WithMessage("Brand name cannot exceed 120 characters.");

        RuleFor(static x => x.Slug)
            .NotEmpty().WithMessage("Slug cannot be empty.")
            .MaximumLength(150).WithMessage("Slug cannot exceed 150 characters.");
    }
}

public sealed class CreateBannerRequestValidator : AbstractValidator<CreateBannerRequest>
{
    public CreateBannerRequestValidator()
    {
        RuleFor(static x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề banner không được để trống.")
            .MaximumLength(200).WithMessage("Tiêu đề không được vượt quá 200 ký tự.");

        RuleFor(static x => x.ImageKey)
            .NotEmpty().WithMessage("Hình ảnh banner không được để trống.")
            .MaximumLength(500).WithMessage("Đường dẫn hình ảnh không được vượt quá 500 ký tự.");

        RuleFor(static x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Thứ tự hiển thị phải lớn hơn hoặc bằng 0.");
    }
}

public sealed class UpdateBannerRequestValidator : AbstractValidator<UpdateBannerRequest>
{
    public UpdateBannerRequestValidator()
    {
        RuleFor(static x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề banner không được để trống.")
            .MaximumLength(200).WithMessage("Tiêu đề không được vượt quá 200 ký tự.");

        RuleFor(static x => x.ImageKey)
            .NotEmpty().WithMessage("Hình ảnh banner không được để trống.")
            .MaximumLength(500).WithMessage("Đường dẫn hình ảnh không được vượt quá 500 ký tự.");

        RuleFor(static x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Thứ tự hiển thị phải lớn hơn hoặc bằng 0.");
    }
}
