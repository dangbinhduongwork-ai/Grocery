using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GroceryHub.Modules.Reviews.Contracts;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using Xunit;

namespace GroceryHub.IntegrationTests.Reviews;

[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class ReviewsIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
{
    private const string DefaultPassword = "Password@123456";

    private readonly SqlServerTestDatabaseFixture _dbFixture = dbFixture;
    private GroceryHubWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _dbFixture.ResetDatabaseAsync();
        _factory = new GroceryHubWebApplicationFactory(_dbFixture.ConnectionString);
        _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task StorefrontReviews_PublicCanRead_VerifiedPurchaseEnforced()
    {
        // 1. Arrange Product and Customer
        Guid productId;
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            var (_, pId) = await Phase07TestHelper.SeedCategoryAndProductAsync(
                db,
                "Sữa chua Vinamilk 100g",
                $"SKU-YOG-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
                sellingPrice: 8_000L,
                costPrice: 6_000L,
                onHand: 100,
                reserved: 0);
            productId = pId;
        }

        string email = $"cust.review.{Guid.NewGuid():N}@groceryhub.vn";
        var (userId, token) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory,
            _client,
            email,
            "Khách Hàng Đánh Giá",
            "0909123456");

        // 2. Public reads product reviews before any review -> empty 200 OK
        using var publicReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/products/{productId}/reviews");
        using var publicRes = await _client.SendAsync(publicReq);
        Assert.Equal(HttpStatusCode.OK, publicRes.StatusCode);

        var pubData = await publicRes.Content.ReadFromJsonAsync<ProductReviewsResponseDto>();
        Assert.NotNull(pubData);
        Assert.Equal(0, pubData.Summary.RatingCount);
        Assert.Empty(pubData.Reviews.Items);

        // 3. Create an order that is NOT yet Delivered (e.g. Placed)
        Guid orderId = Guid.NewGuid();
        Guid orderItemId = Guid.NewGuid();
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            var order = new Order
            {
                Id = orderId,
                OrderNumber = $"GH{Random.Shared.Next(10000000, 99999999)}",
                UserId = userId,
                Status = OrderStatusConstants.Placed,
                ShippingRecipientName = "Khách Hàng Đánh Giá",
                ShippingPhone = "0909123456",
                ShippingProvinceCode = "01",
                ShippingWardCode = "00001",
                Total = 16_000L,
                Subtotal = 16_000L,
                CreatedAt = DateTimeOffset.UtcNow
            };
            var orderItem = new OrderItem
            {
                Id = orderItemId,
                OrderId = orderId,
                ProductId = productId,
                ProductName = "Sữa chua Vinamilk 100g",
                ProductSku = "SKU-YOG",
                ProductUnit = "Hộp",
                UnitPrice = 8_000L,
                Quantity = 2,
                Subtotal = 16_000L,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Orders.Add(order);
            db.OrderItems.Add(orderItem);
            await db.SaveChangesAsync();
        }

        // 4. Try to submit review while order is not Delivered -> 403 Forbidden (REVIEW_NOT_ELIGIBLE)
        using var unearnedReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/products/{productId}/reviews");
        unearnedReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        unearnedReq.Content = JsonContent.Create(new CreateReviewRequest(
            orderItemId,
            5,
            "Sữa chua ngon",
            "Sữa chua ăn rất vừa miệng, date xa"));

        using var unearnedRes = await _client.SendAsync(unearnedReq);
        Assert.Equal(HttpStatusCode.Forbidden, unearnedRes.StatusCode);

        // 5. Update order to Delivered
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            var order = await db.Orders.FindAsync(orderId);
            Assert.NotNull(order);
            order.Status = OrderStatusConstants.Delivered;
            await db.SaveChangesAsync();
        }

        // 6. Submit review again -> 201 Created (Pending)
        using var reviewReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/products/{productId}/reviews");
        reviewReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        reviewReq.Content = JsonContent.Create(new CreateReviewRequest(
            orderItemId,
            5,
            "Sữa chua ngon",
            "Sữa chua ăn rất vừa miệng, date xa"));

        using var reviewRes = await _client.SendAsync(reviewReq);
        Assert.Equal(HttpStatusCode.Created, reviewRes.StatusCode);

        var reviewDto = await reviewRes.Content.ReadFromJsonAsync<CustomerReviewDto>();
        Assert.NotNull(reviewDto);
        Assert.Equal(ReviewStatuses.Pending, reviewDto.Status);
        Assert.Equal(5, reviewDto.Rating);
        Guid reviewId = reviewDto.Id;

        // 7. Try duplicate review for same order item -> 409 Conflict
        using var dupReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/products/{productId}/reviews");
        dupReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        dupReq.Content = JsonContent.Create(new CreateReviewRequest(
            orderItemId,
            4,
            "Thử đánh giá lại",
            "Không được phép"));

        using var dupRes = await _client.SendAsync(dupReq);
        Assert.Equal(HttpStatusCode.Conflict, dupRes.StatusCode);

        // 8. Admin/Manager approves review
        string adminToken = await Phase07TestHelper.LoginAdminAsync(_client);

        using var approveReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/reviews/{reviewId}/approve");
        approveReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var approveRes = await _client.SendAsync(approveReq);
        Assert.Equal(HttpStatusCode.OK, approveRes.StatusCode);

        var approvedDto = await approveRes.Content.ReadFromJsonAsync<AdminReviewDto>();
        Assert.NotNull(approvedDto);
        Assert.Equal(ReviewStatuses.Approved, approvedDto.Status);

        // 9. Verify product aggregates updated
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            var prod = await db.Products.FindAsync(productId);
            Assert.NotNull(prod);
            Assert.Equal(1, prod.RatingCount);
            Assert.Equal(5.00m, prod.RatingAvg);
        }

        // 10. Storefront public view now contains the approved review
        using var pubReq2 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/products/{productId}/reviews");
        using var pubRes2 = await _client.SendAsync(pubReq2);
        var pubData2 = await pubRes2.Content.ReadFromJsonAsync<ProductReviewsResponseDto>();
        Assert.NotNull(pubData2);
        Assert.Equal(1, pubData2.Summary.RatingCount);
        Assert.Equal(5.00m, pubData2.Summary.RatingAvg);
        Assert.Single(pubData2.Reviews.Items);
    }
}
