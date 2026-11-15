using GroceryHub.Modules.Reviews.Contracts;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace GroceryHub.Modules.Reviews.Application.Services;

public sealed class ReviewsService
{
    private readonly GroceryHubDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly IAuditLogPort _auditLogPort;

    public ReviewsService(
        GroceryHubDbContext dbContext,
        TimeProvider timeProvider,
        IAuditLogPort auditLogPort)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _auditLogPort = auditLogPort ?? throw new ArgumentNullException(nameof(auditLogPort));
    }

    public async Task<Result<ProductReviewsResponseDto>> GetProductReviewsAsync(
        Guid productId,
        int? ratingFilter,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        bool productExists = await _dbContext.Products
            .AsNoTracking()
            .AnyAsync(p => p.Id == productId, ct);

        if (!productExists)
        {
            return Result.Failure<ProductReviewsResponseDto>(
                DomainError.NotFound($"Sản phẩm với ID '{productId}' không tồn tại."));
        }

        var baseQuery = _dbContext.Reviews
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.ProductId == productId && r.Status == ReviewStatus.Approved);

        var allRatings = await baseQuery.Select(r => r.Rating).ToListAsync(ct);

        var starCounts = new Dictionary<int, int>
        {
            [1] = allRatings.Count(r => r == 1),
            [2] = allRatings.Count(r => r == 2),
            [3] = allRatings.Count(r => r == 3),
            [4] = allRatings.Count(r => r == 4),
            [5] = allRatings.Count(r => r == 5),
        };

        int totalCount = allRatings.Count;
        decimal ratingAvg = totalCount > 0
            ? Math.Round((decimal)allRatings.Average(), 2, MidpointRounding.AwayFromZero)
            : 0m;

        var query = baseQuery;
        if (ratingFilter.HasValue && ratingFilter.Value >= 1 && ratingFilter.Value <= 5)
        {
            query = query.Where(r => r.Rating == ratingFilter.Value);
        }

        int filteredCount = await query.CountAsync(ct);
        int validPage = Math.Max(1, page);
        int validPageSize = Math.Clamp(pageSize, 1, 50);

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((validPage - 1) * validPageSize)
            .Take(validPageSize)
            .Select(r => new PublicReviewDto(
                r.Id,
                MaskAuthorName(r.User != null ? r.User.FullName : "Khách hàng"),
                r.Rating,
                r.Title,
                r.Body,
                r.CreatedAt,
                r.EditedAt,
                r.ReplyBody,
                r.ReplyAt))
            .ToListAsync(ct);

        var summary = new ReviewSummaryDto(ratingAvg, totalCount, starCounts);
        var pagedReviews = PagedResponse<PublicReviewDto>.Create(
            reviews,
            validPage,
            validPageSize,
            filteredCount);

        return Result.Success(new ProductReviewsResponseDto(summary, pagedReviews));
    }

    public async Task<Result<CustomerReviewDto>> CreateReviewAsync(
        Guid productId,
        Guid userId,
        CreateReviewRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var orderItem = await _dbContext.OrderItems
            .AsNoTracking()
            .Include(oi => oi.Order)
            .FirstOrDefaultAsync(oi => oi.Id == request.OrderItemId, ct);

        if (orderItem is null || orderItem.Order is null ||
            orderItem.Order.UserId != userId ||
            orderItem.ProductId != productId ||
            orderItem.Order.Status != OrderStatusConstants.Delivered)
        {
            return Result.Failure<CustomerReviewDto>(
                DomainError.Forbidden("REVIEW_NOT_ELIGIBLE", "Chỉ khách hàng đã nhận đơn hàng thành công mới có thể đánh giá sản phẩm này."));
        }

        bool alreadyReviewed = await _dbContext.Reviews
            .AnyAsync(r => r.OrderItemId == request.OrderItemId, ct);

        if (alreadyReviewed)
        {
            return Result.Failure<CustomerReviewDto>(
                DomainError.Conflict("REVIEW_ALREADY_EXISTS", "Mục đơn hàng này đã được đánh giá."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();

        var review = new Review
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            UserId = userId,
            OrderItemId = request.OrderItemId,
            Rating = request.Rating,
            Title = request.Title?.Trim(),
            Body = request.Body.Trim(),
            Status = ReviewStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Reviews.Add(review);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(new CustomerReviewDto(
            review.Id,
            review.ProductId,
            review.OrderItemId,
            review.Rating,
            review.Title,
            review.Body,
            review.Status,
            review.CreatedAt,
            review.EditedAt,
            review.ReplyBody,
            review.ReplyAt));
    }

    public async Task<Result<CustomerReviewDto>> UpdateReviewAsync(
        Guid reviewId,
        Guid userId,
        UpdateReviewRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var review = await _dbContext.Reviews
            .FirstOrDefaultAsync(r => r.Id == reviewId, ct);

        if (review is null)
        {
            return Result.Failure<CustomerReviewDto>(
                DomainError.NotFound($"Đánh giá với ID '{reviewId}' không tồn tại."));
        }

        if (review.UserId != userId)
        {
            return Result.Failure<CustomerReviewDto>(
                DomainError.Forbidden("FORBIDDEN", "Bạn không có quyền chỉnh sửa đánh giá này."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        if (now - review.CreatedAt > TimeSpan.FromDays(7))
        {
            return Result.Failure<CustomerReviewDto>(
                DomainError.Conflict("EDIT_WINDOW_EXPIRED", "Đánh giá chỉ có thể chỉnh sửa trong vòng 7 ngày kể từ khi tạo."));
        }

        string previousStatus = review.Status;

        review.Rating = request.Rating;
        review.Title = request.Title?.Trim();
        review.Body = request.Body.Trim();
        review.Status = ReviewStatus.Pending;
        review.ModeratedBy = null;
        review.ModeratedAt = null;
        review.RejectionReason = null;
        review.EditedAt = now;
        review.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(ct);

        if (previousStatus == ReviewStatus.Approved)
        {
            await RecalculateProductRatingAggregatesAsync(review.ProductId, ct);
        }

        return Result.Success(new CustomerReviewDto(
            review.Id,
            review.ProductId,
            review.OrderItemId,
            review.Rating,
            review.Title,
            review.Body,
            review.Status,
            review.CreatedAt,
            review.EditedAt,
            review.ReplyBody,
            review.ReplyAt));
    }

    public async Task<Result<PagedResponse<CustomerReviewDto>>> GetCustomerReviewsAsync(
        Guid userId,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        int validPage = Math.Max(1, page);
        int validPageSize = Math.Clamp(pageSize, 1, 50);

        var query = _dbContext.Reviews
            .AsNoTracking()
            .Where(r => r.UserId == userId);

        int totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((validPage - 1) * validPageSize)
            .Take(validPageSize)
            .Select(r => new CustomerReviewDto(
                r.Id,
                r.ProductId,
                r.OrderItemId,
                r.Rating,
                r.Title,
                r.Body,
                r.Status,
                r.CreatedAt,
                r.EditedAt,
                r.ReplyBody,
                r.ReplyAt))
            .ToListAsync(ct);

        return Result.Success(PagedResponse<CustomerReviewDto>.Create(
            items,
            validPage,
            validPageSize,
            totalCount));
    }

    public async Task<Result<PagedResponse<AdminReviewDto>>> GetAdminReviewsAsync(
        string? status,
        Guid? productId,
        int? rating,
        int page = 1,
        int pageSize = 24,
        CancellationToken ct = default)
    {
        int validPage = Math.Max(1, page);
        int validPageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Reviews
            .AsNoTracking()
            .Include(r => r.Product)
            .Include(r => r.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (productId.HasValue)
        {
            query = query.Where(r => r.ProductId == productId.Value);
        }

        if (rating.HasValue)
        {
            query = query.Where(r => r.Rating == rating.Value);
        }

        int totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((validPage - 1) * validPageSize)
            .Take(validPageSize)
            .Select(r => new AdminReviewDto(
                r.Id,
                r.ProductId,
                r.Product != null ? r.Product.Name : string.Empty,
                r.UserId,
                r.User != null ? r.User.FullName : string.Empty,
                r.OrderItemId,
                r.Rating,
                r.Title,
                r.Body,
                r.Status,
                r.RejectionReason,
                r.ReplyBody,
                r.ModeratedBy,
                r.ModeratedAt,
                r.CreatedAt,
                r.UpdatedAt))
            .ToListAsync(ct);

        return Result.Success(PagedResponse<AdminReviewDto>.Create(
            items,
            validPage,
            validPageSize,
            totalCount));
    }

    public async Task<Result<AdminReviewDto>> ApproveReviewAsync(
        Guid reviewId,
        Guid moderatorUserId,
        CancellationToken ct = default)
    {
        var review = await _dbContext.Reviews
            .Include(r => r.Product)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == reviewId, ct);

        if (review is null)
        {
            return Result.Failure<AdminReviewDto>(
                DomainError.NotFound($"Đánh giá với ID '{reviewId}' không tồn tại."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        review.Status = ReviewStatus.Approved;
        review.ModeratedBy = moderatorUserId;
        review.ModeratedAt = now;
        review.RejectionReason = null;
        review.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(ct);

        await RecalculateProductRatingAggregatesAsync(review.ProductId, ct);

        await _auditLogPort.WriteAsync(
            new WriteAuditLogCommand(
                moderatorUserId,
                null,
                "Approve",
                "Review",
                review.Id.ToString(),
                null,
                new { status = "Approved", productId = review.ProductId }),
            ct);

        return Result.Success(new AdminReviewDto(
            review.Id,
            review.ProductId,
            review.Product != null ? review.Product.Name : string.Empty,
            review.UserId,
            review.User != null ? review.User.FullName : string.Empty,
            review.OrderItemId,
            review.Rating,
            review.Title,
            review.Body,
            review.Status,
            review.RejectionReason,
            review.ReplyBody,
            review.ModeratedBy,
            review.ModeratedAt,
            review.CreatedAt,
            review.UpdatedAt));
    }

    public async Task<Result<AdminReviewDto>> RejectReviewAsync(
        Guid reviewId,
        Guid moderatorUserId,
        string? reason,
        CancellationToken ct = default)
    {
        var review = await _dbContext.Reviews
            .Include(r => r.Product)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == reviewId, ct);

        if (review is null)
        {
            return Result.Failure<AdminReviewDto>(
                DomainError.NotFound($"Đánh giá với ID '{reviewId}' không tồn tại."));
        }

        string previousStatus = review.Status;
        DateTimeOffset now = _timeProvider.GetUtcNow();
        review.Status = ReviewStatus.Rejected;
        review.ModeratedBy = moderatorUserId;
        review.ModeratedAt = now;
        review.RejectionReason = reason?.Trim();
        review.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(ct);

        if (previousStatus == ReviewStatus.Approved)
        {
            await RecalculateProductRatingAggregatesAsync(review.ProductId, ct);
        }

        await _auditLogPort.WriteAsync(
            new WriteAuditLogCommand(
                moderatorUserId,
                null,
                "Reject",
                "Review",
                review.Id.ToString(),
                null,
                new { status = "Rejected", reason }),
            ct);

        return Result.Success(new AdminReviewDto(
            review.Id,
            review.ProductId,
            review.Product != null ? review.Product.Name : string.Empty,
            review.UserId,
            review.User != null ? review.User.FullName : string.Empty,
            review.OrderItemId,
            review.Rating,
            review.Title,
            review.Body,
            review.Status,
            review.RejectionReason,
            review.ReplyBody,
            review.ModeratedBy,
            review.ModeratedAt,
            review.CreatedAt,
            review.UpdatedAt));
    }

    public async Task<Result<AdminReviewDto>> ReplyReviewAsync(
        Guid reviewId,
        Guid replierUserId,
        string replyBody,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replyBody);

        var review = await _dbContext.Reviews
            .Include(r => r.Product)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == reviewId, ct);

        if (review is null)
        {
            return Result.Failure<AdminReviewDto>(
                DomainError.NotFound($"Đánh giá với ID '{reviewId}' không tồn tại."));
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        review.ReplyBody = replyBody.Trim();
        review.ReplyBy = replierUserId;
        review.ReplyAt = now;
        review.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(ct);

        await _auditLogPort.WriteAsync(
            new WriteAuditLogCommand(
                replierUserId,
                null,
                "Reply",
                "Review",
                review.Id.ToString(),
                null,
                new { replyLength = replyBody.Length }),
            ct);

        return Result.Success(new AdminReviewDto(
            review.Id,
            review.ProductId,
            review.Product != null ? review.Product.Name : string.Empty,
            review.UserId,
            review.User != null ? review.User.FullName : string.Empty,
            review.OrderItemId,
            review.Rating,
            review.Title,
            review.Body,
            review.Status,
            review.RejectionReason,
            review.ReplyBody,
            review.ModeratedBy,
            review.ModeratedAt,
            review.CreatedAt,
            review.UpdatedAt));
    }

    private async Task RecalculateProductRatingAggregatesAsync(Guid productId, CancellationToken ct)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId, ct);
        if (product is null)
        {
            return;
        }

        var approvedRatings = await _dbContext.Reviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId && r.Status == ReviewStatus.Approved)
            .Select(r => r.Rating)
            .ToListAsync(ct);

        if (approvedRatings.Count == 0)
        {
            product.RatingAvg = 0m;
            product.RatingCount = 0;
        }
        else
        {
            product.RatingCount = approvedRatings.Count;
            product.RatingAvg = Math.Round((decimal)approvedRatings.Average(), 2, MidpointRounding.AwayFromZero);
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    private static string MaskAuthorName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "Khách hàng";
        }

        string[] parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return parts[0].Length <= 2 ? parts[0] : $"{parts[0][..1]}***";
        }

        return $"{parts[0]} {parts[^1][..1]}***";
    }
}
