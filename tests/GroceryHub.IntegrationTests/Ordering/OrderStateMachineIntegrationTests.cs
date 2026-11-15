using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GroceryHub.Modules.Ordering.Application.DTOs;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GroceryHub.IntegrationTests.Ordering;

[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class OrderStateMachineIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
{
    private readonly SqlServerTestDatabaseFixture _dbFixture = dbFixture;
    private GroceryHubWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private string _adminToken = null!;
    private string _staffToken = null!;
    private Guid _staffId;

    public async Task InitializeAsync()
    {
        await _dbFixture.ResetDatabaseAsync();
        _factory = new GroceryHubWebApplicationFactory(_dbFixture.ConnectionString);
        _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        _adminToken = await Phase07TestHelper.LoginAdminAsync(_client);

        var (staffId, staffToken) = await Phase07TestHelper.CreateAndLoginStaffAsync(
            _client,
            _adminToken,
            $"staff.sm.{Guid.NewGuid():N}@groceryhub.vn",
            "Staff StateMachine",
            $"SM-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}",
            role: "Staff",
            extraPermissions: ["orders.process", "orders.ship"]);

        _staffId = staffId;
        _staffToken = staffToken;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<(Guid OrderId, Guid ProductId, string CustomerToken, Guid CustomerId, long Total)> PlaceSampleOrderAsync(
        string? couponCode = null)
    {
        Guid productId;
        const long sellingPrice = 180_000L;
        const long costPrice = 140_000L;

        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            var (_, pId) = await Phase07TestHelper.SeedCategoryAndProductAsync(
                db,
                "Hạt nêm Knorr 900g",
                $"SKU-KNORR-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                sellingPrice,
                costPrice,
                onHand: 20,
                reserved: 0);
            productId = pId;
        }

        string email = $"sm.cust.{Guid.NewGuid():N}@groceryhub.vn";
        var (userId, customerToken) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory,
            _client,
            email,
            "Customer State Machine",
            $"091777{Random.Shared.Next(1000, 9999)}");

        Guid addressId = await Phase07TestHelper.CreateAddressAsync(
            _client,
            customerToken,
            recipientName: "Customer SM",
            phone: "0917771234",
            provinceCode: "79",
            wardCode: "79001",
            street: "123 Le Loi, Ben Nghe, Quan 1");

        // Add 2 items to cart
        AddCartItemRequest addReq = new(productId, 2);
        using HttpRequestMessage addCartMsg = new(HttpMethod.Post, "/api/v1/cart/items")
        {
            Content = JsonContent.Create(addReq)
        };
        addCartMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        using HttpResponseMessage addCartRes = await _client.SendAsync(addCartMsg);
        Assert.Equal(HttpStatusCode.OK, addCartRes.StatusCode);

        string idempotencyKey = Guid.NewGuid().ToString("N");
        PlaceOrderRequest orderReq = new(addressId, couponCode, "COD", "Sample order for state machine");

        using HttpRequestMessage placeOrderMsg = new(HttpMethod.Post, "/api/v1/checkout/orders")
        {
            Content = JsonContent.Create(orderReq)
        };
        placeOrderMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        placeOrderMsg.Headers.Add("Idempotency-Key", idempotencyKey);
        using HttpResponseMessage placeOrderRes = await _client.SendAsync(placeOrderMsg);
        Assert.Equal(HttpStatusCode.Created, placeOrderRes.StatusCode);

        PlaceOrderResponseDto? placed = await placeOrderRes.Content.ReadFromJsonAsync<PlaceOrderResponseDto>();
        Assert.NotNull(placed);

        return (placed!.Order.Id, productId, customerToken, userId, placed.Order.Total);
    }

    [Fact]
    public async Task ValidStaffTransitions_PlacedThroughDelivered_SucceedsWithCorrectStates()
    {
        var (orderId, productId, _, _, total) = await PlaceSampleOrderAsync();

        // 1. Placed -> Confirmed
        TransitionOrderRequest confirmReq = new("Confirmed", "Xac nhan don hang", null);
        using HttpRequestMessage confirmMsg = new(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(confirmReq)
        };
        confirmMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _staffToken);
        using HttpResponseMessage confirmRes = await _client.SendAsync(confirmMsg);
        Assert.Equal(HttpStatusCode.OK, confirmRes.StatusCode);

        // 2. Confirmed -> Preparing
        TransitionOrderRequest prepReq = new("Preparing", "Dang nhat hang", null);
        using HttpRequestMessage prepMsg = new(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(prepReq)
        };
        prepMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _staffToken);
        using HttpResponseMessage prepRes = await _client.SendAsync(prepMsg);
        Assert.Equal(HttpStatusCode.OK, prepRes.StatusCode);

        // 3. Preparing -> Packed
        TransitionOrderRequest packReq = new("Packed", "Da dong goi", null);
        using HttpRequestMessage packMsg = new(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(packReq)
        };
        packMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _staffToken);
        using HttpResponseMessage packRes = await _client.SendAsync(packMsg);
        Assert.Equal(HttpStatusCode.OK, packRes.StatusCode);

        // 4. Packed -> Shipping
        TransitionOrderRequest shipReq = new("Shipping", "Xuat kho giao hang", null);
        using HttpRequestMessage shipMsg = new(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(shipReq)
        };
        shipMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _staffToken);
        using HttpResponseMessage shipRes = await _client.SendAsync(shipMsg);
        Assert.Equal(HttpStatusCode.OK, shipRes.StatusCode);

        // 5. Shipping -> DeliveryFailed
        TransitionOrderRequest failReq = new("DeliveryFailed", "Khach hen lai gio khac", null);
        using HttpRequestMessage failMsg = new(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(failReq)
        };
        failMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _staffToken);
        using HttpResponseMessage failRes = await _client.SendAsync(failMsg);
        Assert.Equal(HttpStatusCode.OK, failRes.StatusCode);

        // 6. DeliveryFailed -> Shipping (Retry delivery)
        TransitionOrderRequest retryReq = new("Shipping", "Giao lai lan 2", null);
        using HttpRequestMessage retryMsg = new(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(retryReq)
        };
        retryMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _staffToken);
        using HttpResponseMessage retryRes = await _client.SendAsync(retryMsg);
        Assert.Equal(HttpStatusCode.OK, retryRes.StatusCode);

        // 7. Shipping -> Delivered
        TransitionOrderRequest deliverReq = new("Delivered", "Giao thanh cong", total);
        using HttpRequestMessage deliverMsg = new(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(deliverReq)
        };
        deliverMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _staffToken);
        using HttpResponseMessage deliverRes = await _client.SendAsync(deliverMsg);
        Assert.Equal(HttpStatusCode.OK, deliverRes.StatusCode);

        // Verify Delivered in DB
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            Order order = await db.Orders.FirstAsync(o => o.Id == orderId);
            Assert.Equal("Delivered", order.Status);
            Assert.Equal("Paid", order.PaymentStatus);
        }
    }

    [Fact]
    public async Task InvalidTransitions_Return409Conflict_OrderInvalidTransition()
    {
        var (orderId, _, _, _, _) = await PlaceSampleOrderAsync();

        // 1. Placed -> Packed is invalid (must go Placed -> Confirmed -> Preparing -> Packed)
        TransitionOrderRequest invalid1 = new("Packed", "Skip states", null);
        using HttpRequestMessage msg1 = new(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(invalid1)
        };
        msg1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _staffToken);
        using HttpResponseMessage res1 = await _client.SendAsync(msg1);
        string body1 = await res1.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Conflict, res1.StatusCode);
        Assert.Contains("ORDER_INVALID_TRANSITION", body1);

        // 2. Placed -> Delivered is invalid
        TransitionOrderRequest invalid2 = new("Delivered", "Direct delivery", 100000);
        using HttpRequestMessage msg2 = new(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(invalid2)
        };
        msg2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _staffToken);
        using HttpResponseMessage res2 = await _client.SendAsync(msg2);
        string body2 = await res2.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Conflict, res2.StatusCode);
        Assert.Contains("ORDER_INVALID_TRANSITION", body2);

        // Move order to Confirmed
        TransitionOrderRequest confirmReq = new("Confirmed", "Xac nhan", null);
        using HttpRequestMessage confirmMsg = new(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(confirmReq)
        };
        confirmMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _staffToken);
        using HttpResponseMessage confirmRes = await _client.SendAsync(confirmMsg);
        Assert.Equal(HttpStatusCode.OK, confirmRes.StatusCode);

        // 3. Confirmed -> Shipping is invalid (must go through Preparing and Packed)
        TransitionOrderRequest invalid3 = new("Shipping", "Direct ship", null);
        using HttpRequestMessage msg3 = new(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(invalid3)
        };
        msg3.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _staffToken);
        using HttpResponseMessage res3 = await _client.SendAsync(msg3);
        string body3 = await res3.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Conflict, res3.StatusCode);
        Assert.Contains("ORDER_INVALID_TRANSITION", body3);
    }

    [Fact]
    public async Task CustomerCancel_InPlacedOrConfirmed_ReleasesStockAndRestoresCoupon()
    {
        // Arrange with coupon
        string couponCode = $"COUPON-CANCEL-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid couponId = Guid.NewGuid();

        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            Coupon coupon = new()
            {
                Id = couponId,
                Code = couponCode,
                Name = "Coupon Test Cancel",
                Description = "Giam 20K",
                Type = "FixedAmount",
                Value = 20_000L,
                MinSubtotal = 100_000L,
                StartsAt = now.AddDays(-1),
                EndsAt = now.AddDays(10),
                UsageLimit = 10,
                PerUserLimit = 1,
                UsedCount = 0,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Coupons.Add(coupon);
            await db.SaveChangesAsync();
        }

        var (orderId, productId, customerToken, _, _) = await PlaceSampleOrderAsync(couponCode);

        // Verify coupon used count = 1 before cancel
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            Coupon c = await db.Coupons.FirstAsync(x => x.Id == couponId);
            Assert.Equal(1, c.UsedCount);

            StockItem s = await db.StockItems.FirstAsync(x => x.ProductId == productId);
            Assert.Equal(2, s.Reserved);
        }

        // Customer cancels order in Placed state
        CancelOrderRequest cancelReq = new("Khong con nhu cau su dung");
        using HttpRequestMessage cancelMsg = new(HttpMethod.Post, $"/api/v1/me/orders/{orderId}/cancel")
        {
            Content = JsonContent.Create(cancelReq)
        };
        cancelMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        using HttpResponseMessage cancelRes = await _client.SendAsync(cancelMsg);
        Assert.Equal(HttpStatusCode.OK, cancelRes.StatusCode);

        // Verify DB: Order is Cancelled, Stock reservation is Released, Coupon UsedCount restored to 0
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            Order order = await db.Orders.FirstAsync(o => o.Id == orderId);
            Assert.Equal("Cancelled", order.Status);
            Assert.Equal("Khong con nhu cau su dung", order.CancellationReason);

            StockItem s = await db.StockItems.FirstAsync(x => x.ProductId == productId);
            Assert.Equal(0, s.Reserved);

            StockReservation r = await db.StockReservations.FirstAsync(x => x.OrderId == orderId);
            Assert.Equal("Released", r.Status);

            Coupon c = await db.Coupons.FirstAsync(x => x.Id == couponId);
            Assert.Equal(0, c.UsedCount);

            CouponRedemption redemption = await db.CouponRedemptions.FirstAsync(x => x.OrderId == orderId);
            Assert.NotNull(redemption.ReleasedAt);
        }
    }

    [Fact]
    public async Task CustomerCancel_RejectedOnceInPreparingPackedOrShipping_Returns409()
    {
        var (orderId, _, customerToken, _, _) = await PlaceSampleOrderAsync();

        // Transition Placed -> Confirmed -> Preparing
        TransitionOrderRequest confirmReq = new("Confirmed", "Xac nhan", null);
        using HttpRequestMessage confirmMsg = new(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(confirmReq)
        };
        confirmMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _staffToken);
        using HttpResponseMessage confirmRes = await _client.SendAsync(confirmMsg);
        Assert.Equal(HttpStatusCode.OK, confirmRes.StatusCode);

        TransitionOrderRequest prepReq = new("Preparing", "Bat dau nhat hang", null);
        using HttpRequestMessage prepMsg = new(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(prepReq)
        };
        prepMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _staffToken);
        using HttpResponseMessage prepRes = await _client.SendAsync(prepMsg);
        Assert.Equal(HttpStatusCode.OK, prepRes.StatusCode);

        // Customer attempts cancel while in Preparing -> Must be REJECTED 409 ORDER_NOT_CANCELLABLE
        CancelOrderRequest cancelReq = new("Huy vi doi y");
        using HttpRequestMessage cancelMsg = new(HttpMethod.Post, $"/api/v1/me/orders/{orderId}/cancel")
        {
            Content = JsonContent.Create(cancelReq)
        };
        cancelMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        using HttpResponseMessage cancelRes = await _client.SendAsync(cancelMsg);
        string cancelBody = await cancelRes.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, cancelRes.StatusCode);
        Assert.Contains("ORDER_NOT_CANCELLABLE", cancelBody);
    }

    [Fact]
    public async Task DeliveredOrder_EnablesProductReviewEligibility()
    {
        var (orderId, productId, customerToken, customerId, total) = await PlaceSampleOrderAsync();

        // 1. Move to Shipping
        TransitionOrderRequest confirmReq = new("Confirmed", "Confirm", null);
        using HttpRequestMessage confirmMsg = new(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(confirmReq)
        };
        confirmMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _staffToken);
        await _client.SendAsync(confirmMsg);

        TransitionOrderRequest prepReq = new("Preparing", "Preparing", null);
        using HttpRequestMessage prepMsg = new(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(prepReq)
        };
        prepMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _staffToken);
        await _client.SendAsync(prepMsg);

        TransitionOrderRequest packReq = new("Packed", "Packed", null);
        using HttpRequestMessage packMsg = new(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(packReq)
        };
        packMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _staffToken);
        await _client.SendAsync(packMsg);

        TransitionOrderRequest shipReq = new("Shipping", "Shipping", null);
        using HttpRequestMessage shipMsg = new(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(shipReq)
        };
        shipMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _staffToken);
        await _client.SendAsync(shipMsg);

        // Check review eligibility while in Shipping: Customer is NOT eligible because order is not Delivered
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            bool isEligibleWhileShipping = await db.Orders
                .Where(o => o.UserId == customerId && o.Status == "Delivered")
                .SelectMany(o => o.Items)
                .AnyAsync(i => i.ProductId == productId);

            Assert.False(isEligibleWhileShipping);
        }

        // 2. Transition Shipping -> Delivered
        TransitionOrderRequest deliverReq = new("Delivered", "Delivered success", total);
        using HttpRequestMessage deliverMsg = new(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(deliverReq)
        };
        deliverMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _staffToken);
        using HttpResponseMessage deliverRes = await _client.SendAsync(deliverMsg);
        Assert.Equal(HttpStatusCode.OK, deliverRes.StatusCode);

        // 3. Check review eligibility after Delivered: Customer IS eligible!
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            bool isEligibleAfterDelivered = await db.Orders
                .Where(o => o.UserId == customerId && o.Status == "Delivered")
                .SelectMany(o => o.Items)
                .AnyAsync(i => i.ProductId == productId);

            Assert.True(isEligibleAfterDelivered);
        }
    }
}
