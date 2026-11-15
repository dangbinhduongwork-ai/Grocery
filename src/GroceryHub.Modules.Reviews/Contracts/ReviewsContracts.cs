using GroceryHub.SharedKernel;

namespace GroceryHub.Modules.Reviews.Contracts;

public static class ReviewStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";

    public static readonly IReadOnlyList<string> All = [Pending, Approved, Rejected];
}

public sealed record PublicReviewDto(
    Guid Id,
    string AuthorName,
    int Rating,
    string? Title,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EditedAt,
    string? ReplyBody,
    DateTimeOffset? ReplyAt);

public sealed record ReviewSummaryDto(
    decimal RatingAvg,
    int RatingCount,
    IReadOnlyDictionary<int, int> StarCounts);

public sealed record ProductReviewsResponseDto(
    ReviewSummaryDto Summary,
    PagedResponse<PublicReviewDto> Reviews);

public sealed record CustomerReviewDto(
    Guid Id,
    Guid ProductId,
    Guid OrderItemId,
    int Rating,
    string? Title,
    string Body,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EditedAt,
    string? ReplyBody,
    DateTimeOffset? ReplyAt);

public sealed record AdminReviewDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    Guid UserId,
    string CustomerName,
    Guid OrderItemId,
    int Rating,
    string? Title,
    string Body,
    string Status,
    string? RejectionReason,
    string? ReplyBody,
    Guid? ModeratedBy,
    DateTimeOffset? ModeratedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record CreateReviewRequest(
    Guid OrderItemId,
    int Rating,
    string? Title,
    string Body);

public sealed record UpdateReviewRequest(
    int Rating,
    string? Title,
    string Body);

public sealed record RejectReviewRequest(
    string? Reason);

public sealed record ReplyReviewRequest(
    string ReplyBody);
