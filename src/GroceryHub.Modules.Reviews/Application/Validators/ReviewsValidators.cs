using FluentValidation;
using GroceryHub.Modules.Reviews.Contracts;

namespace GroceryHub.Modules.Reviews.Application.Validators;

public sealed class CreateReviewValidator : AbstractValidator<CreateReviewRequest>
{
    public CreateReviewValidator()
    {
        RuleFor(x => x.OrderItemId)
            .NotEmpty()
            .WithMessage("Mã mục đơn hàng không được để trống.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Đánh giá phải từ 1 đến 5 sao.");

        RuleFor(x => x.Title)
            .MaximumLength(120)
            .WithMessage("Tiêu đề không được vượt quá 120 ký tự.")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Body)
            .NotEmpty()
            .WithMessage("Nội dung đánh giá không được để trống.")
            .MaximumLength(2000)
            .WithMessage("Nội dung đánh giá không được vượt quá 2000 ký tự.");
    }
}

public sealed class UpdateReviewValidator : AbstractValidator<UpdateReviewRequest>
{
    public UpdateReviewValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Đánh giá phải từ 1 đến 5 sao.");

        RuleFor(x => x.Title)
            .MaximumLength(120)
            .WithMessage("Tiêu đề không được vượt quá 120 ký tự.")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Body)
            .NotEmpty()
            .WithMessage("Nội dung đánh giá không được để trống.")
            .MaximumLength(2000)
            .WithMessage("Nội dung đánh giá không được vượt quá 2000 ký tự.");
    }
}

public sealed class ReplyReviewValidator : AbstractValidator<ReplyReviewRequest>
{
    public ReplyReviewValidator()
    {
        RuleFor(x => x.ReplyBody)
            .NotEmpty()
            .WithMessage("Nội dung phản hồi không được để trống.")
            .MaximumLength(1000)
            .WithMessage("Nội dung phản hồi không được vượt quá 1000 ký tự.");
    }
}

public sealed class RejectReviewValidator : AbstractValidator<RejectReviewRequest>
{
    public RejectReviewValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("Lý do từ chối không được vượt quá 500 ký tự.")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}
