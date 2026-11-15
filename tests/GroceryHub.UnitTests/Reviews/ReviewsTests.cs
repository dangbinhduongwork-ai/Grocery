using FluentValidation.TestHelper;
using GroceryHub.Modules.Reviews.Application.Services;
using GroceryHub.Modules.Reviews.Application.Validators;
using GroceryHub.Modules.Reviews.Contracts;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace GroceryHub.UnitTests.Reviews;

public sealed class ReviewsTests
{
    private readonly CreateReviewValidator _createValidator = new();
    private readonly UpdateReviewValidator _updateValidator = new();
    private readonly RejectReviewValidator _rejectValidator = new();
    private readonly ReplyReviewValidator _replyValidator = new();

    [Fact]
    public void CreateReview_ValidRequest_PassesValidation()
    {
        var request = new CreateReviewRequest(
            Guid.NewGuid(),
            5,
            "Rau rất tươi ngon",
            "Giao hàng nhanh, rau tươi xanh sạch sẽ đóng gói cẩn thận.");

        var result = _createValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void CreateReview_InvalidRating_FailsValidation(int rating)
    {
        var request = new CreateReviewRequest(
            Guid.NewGuid(),
            rating,
            "Tiêu đề",
            "Nội dung đánh giá");

        var result = _createValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Rating);
    }

    [Fact]
    public void CreateReview_EmptyBodyOrOrderItem_FailsValidation()
    {
        var request = new CreateReviewRequest(
            Guid.Empty,
            4,
            null,
            "");

        var result = _createValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.OrderItemId);
        result.ShouldHaveValidationErrorFor(x => x.Body);
    }

    [Fact]
    public void CreateReview_TitleTooLong_FailsValidation()
    {
        var request = new CreateReviewRequest(
            Guid.NewGuid(),
            5,
            new string('A', 121),
            "Nội dung hợp lệ");

        var result = _createValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void CreateReview_BodyTooLong_FailsValidation()
    {
        var request = new CreateReviewRequest(
            Guid.NewGuid(),
            5,
            "Tiêu đề",
            new string('A', 2001));

        var result = _createValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Body);
    }

    [Fact]
    public void ReplyReview_EmptyOrTooLong_FailsValidation()
    {
        var emptyResult = _replyValidator.TestValidate(new ReplyReviewRequest(""));
        emptyResult.ShouldHaveValidationErrorFor(x => x.ReplyBody);

        var tooLongResult = _replyValidator.TestValidate(new ReplyReviewRequest(new string('B', 1001)));
        tooLongResult.ShouldHaveValidationErrorFor(x => x.ReplyBody);
    }

    [Fact]
    public void RejectReview_ReasonTooLong_FailsValidation()
    {
        var tooLongResult = _rejectValidator.TestValidate(new RejectReviewRequest(new string('C', 501)));
        tooLongResult.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public async Task UpdateReview_Within7Days_Succeeds_And_After7Days_FailsWithConflict()
    {
        var options = new DbContextOptionsBuilder<GroceryHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new GroceryHubDbContext(options);
        var initialTime = new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);
        var fakeTime = new FakeTimeProvider(initialTime);
        var auditPort = new TestAuditLogPort();
        var service = new ReviewsService(db, fakeTime, auditPort);

        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();

        var review = new Review
        {
            Id = reviewId,
            ProductId = productId,
            UserId = userId,
            OrderItemId = Guid.NewGuid(),
            Rating = 5,
            Title = "Ban đầu",
            Body = "Nội dung ban đầu",
            Status = ReviewStatus.Approved,
            CreatedAt = initialTime,
            UpdatedAt = initialTime
        };
        db.Reviews.Add(review);
        await db.SaveChangesAsync();

        // 6 days later: should succeed
        fakeTime.Advance(TimeSpan.FromDays(6));
        var updateResult = await service.UpdateReviewAsync(
            reviewId,
            userId,
            new UpdateReviewRequest(4, "Đã cập nhật", "Nội dung mới cập nhật"));

        Assert.True(updateResult.IsSuccess);
        Assert.Equal(ReviewStatus.Pending, updateResult.Value.Status);
        Assert.Equal(4, updateResult.Value.Rating);

        // 8 days later (from creation): should fail
        fakeTime.Advance(TimeSpan.FromDays(3)); // Total 9 days from creation
        var lateResult = await service.UpdateReviewAsync(
            reviewId,
            userId,
            new UpdateReviewRequest(3, "Cố sửa tiếp", "Sửa sau 9 ngày"));

        Assert.True(lateResult.IsFailure);
        Assert.Equal("EDIT_WINDOW_EXPIRED", lateResult.Error.Code);
    }

    [Fact]
    public async Task CreateReview_WhenNotDelivered_FailsWithReviewNotEligible()
    {
        var options = new DbContextOptionsBuilder<GroceryHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new GroceryHubDbContext(options);
        var fakeTime = new FakeTimeProvider();
        var auditPort = new TestAuditLogPort();
        var service = new ReviewsService(db, fakeTime, auditPort);

        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var orderItemId = Guid.NewGuid();

        // Order is still Shipping, not Delivered!
        var order = new Order
        {
            Id = orderId,
            OrderNumber = "ORD-001",
            UserId = userId,
            Status = "Shipping",
            ShippingRecipientName = "Nguyễn Văn A",
            ShippingPhone = "0901234567",
            ShippingProvinceCode = "01",
            ShippingWardCode = "00001",
            Total = 100_000,
            Subtotal = 100_000,
            CreatedAt = fakeTime.GetUtcNow()
        };
        var orderItem = new OrderItem
        {
            Id = orderItemId,
            OrderId = orderId,
            ProductId = productId,
            ProductName = "Rau bina",
            ProductSku = "SKU-001",
            ProductUnit = "Bó",
            UnitPrice = 25_000,
            Quantity = 2,
            Subtotal = 50_000,
            CreatedAt = fakeTime.GetUtcNow()
        };
        db.Orders.Add(order);
        db.OrderItems.Add(orderItem);
        await db.SaveChangesAsync();

        var result = await service.CreateReviewAsync(
            productId,
            userId,
            new CreateReviewRequest(orderItemId, 5, "Ngon", "Nội dung tốt"));

        Assert.True(result.IsFailure);
        Assert.Equal("REVIEW_NOT_ELIGIBLE", result.Error.Code);
    }

    [Fact]
    public async Task CreateReview_WhenDelivered_Succeeds_And_DuplicateFails()
    {
        var options = new DbContextOptionsBuilder<GroceryHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new GroceryHubDbContext(options);
        var fakeTime = new FakeTimeProvider();
        var auditPort = new TestAuditLogPort();
        var service = new ReviewsService(db, fakeTime, auditPort);

        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var orderItemId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            OrderNumber = "ORD-002",
            UserId = userId,
            Status = OrderStatusConstants.Delivered,
            ShippingRecipientName = "Nguyễn Văn B",
            ShippingPhone = "0901234567",
            ShippingProvinceCode = "01",
            ShippingWardCode = "00001",
            Total = 100_000,
            Subtotal = 100_000,
            CreatedAt = fakeTime.GetUtcNow()
        };
        var orderItem = new OrderItem
        {
            Id = orderItemId,
            OrderId = orderId,
            ProductId = productId,
            ProductName = "Rau cải thìa",
            ProductSku = "SKU-002",
            ProductUnit = "Kg",
            UnitPrice = 20_000,
            Quantity = 1,
            Subtotal = 20_000,
            CreatedAt = fakeTime.GetUtcNow()
        };
        db.Orders.Add(order);
        db.OrderItems.Add(orderItem);
        await db.SaveChangesAsync();

        // First review: should succeed
        var firstResult = await service.CreateReviewAsync(
            productId,
            userId,
            new CreateReviewRequest(orderItemId, 5, "Tuyệt vời", "Rất hài lòng chất lượng rau củ."));

        Assert.True(firstResult.IsSuccess);
        Assert.Equal(ReviewStatus.Pending, firstResult.Value.Status);

        // Duplicate review for same order line item: should fail with REVIEW_ALREADY_EXISTS
        var duplicateResult = await service.CreateReviewAsync(
            productId,
            userId,
            new CreateReviewRequest(orderItemId, 4, "Viết lại", "Cố tình đánh giá lần hai"));

        Assert.True(duplicateResult.IsFailure);
        Assert.Equal("REVIEW_ALREADY_EXISTS", duplicateResult.Error.Code);
    }

    [Fact]
    public async Task ApproveReview_UpdatesProductRatingAggregates()
    {
        var options = new DbContextOptionsBuilder<GroceryHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new GroceryHubDbContext(options);
        var fakeTime = new FakeTimeProvider();
        var auditPort = new TestAuditLogPort();
        var service = new ReviewsService(db, fakeTime, auditPort);

        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Sku = "SKU-PROD-01",
            Name = "Cà rốt Đà Lạt",
            Slug = "ca-rot-da-lat",
            Unit = "Kg",
            ListPrice = 30_000,
            SellingPrice = 28_000,
            RatingAvg = 0,
            RatingCount = 0,
            CreatedAt = fakeTime.GetUtcNow()
        };
        db.Products.Add(product);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "customer@example.com",
            NormalizedEmail = "CUSTOMER@EXAMPLE.COM",
            UserName = "customer@example.com",
            NormalizedUserName = "CUSTOMER@EXAMPLE.COM",
            FullName = "Khách Hàng Thử Nghiệm",
            CreatedAt = fakeTime.GetUtcNow()
        };
        db.Users.Add(user);

        var review1 = new Review
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            UserId = userId,
            OrderItemId = Guid.NewGuid(),
            Rating = 5,
            Body = "Đánh giá 5 sao",
            Status = ReviewStatus.Pending,
            CreatedAt = fakeTime.GetUtcNow()
        };
        var review2 = new Review
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            UserId = userId,
            OrderItemId = Guid.NewGuid(),
            Rating = 4,
            Body = "Đánh giá 4 sao",
            Status = ReviewStatus.Pending,
            CreatedAt = fakeTime.GetUtcNow()
        };
        db.Reviews.AddRange(review1, review2);
        await db.SaveChangesAsync();

        var moderatorId = Guid.NewGuid();

        // Approve review 1 (5 stars)
        var approveResult = await service.ApproveReviewAsync(review1.Id, moderatorId);
        Assert.True(approveResult.IsSuccess, approveResult.IsFailure ? approveResult.Error.Message : null);
        var prodAfter1 = await db.Products.FindAsync(productId);
        Assert.NotNull(prodAfter1);
        Assert.Equal(1, prodAfter1.RatingCount);
        Assert.Equal(5.00m, prodAfter1.RatingAvg);

        // Approve review 2 (4 stars) -> avg should be 4.5
        await service.ApproveReviewAsync(review2.Id, moderatorId);
        var prodAfter2 = await db.Products.FindAsync(productId);
        Assert.NotNull(prodAfter2);
        Assert.Equal(2, prodAfter2.RatingCount);
        Assert.Equal(4.50m, prodAfter2.RatingAvg);
    }

    private sealed class TestAuditLogPort : IAuditLogPort
    {
        public Task<Result> WriteAsync(WriteAuditLogCommand command, System.Threading.CancellationToken ct = default)
        {
            return Task.FromResult(Result.Success());
        }
    }
}
