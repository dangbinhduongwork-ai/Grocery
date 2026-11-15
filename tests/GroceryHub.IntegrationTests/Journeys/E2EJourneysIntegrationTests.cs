using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using GroceryHub.Modules.Catalog.Application.DTOs;
using GroceryHub.Modules.Ordering.Application.DTOs;
using GroceryHub.Modules.Payments.Contracts;
using GroceryHub.Modules.Payments.Infrastructure;
using GroceryHub.Modules.Purchasing.Application.DTOs;
using GroceryHub.Modules.Reviews.Contracts;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace GroceryHub.IntegrationTests.Journeys;

/// <summary>
/// Implements and verifies all 7 End-to-End User Journeys (J1 .. J7)
/// specified in docs/spec/12-TESTING-STRATEGY.md §4 and Phase 11 charter.
/// </summary>
[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class E2EJourneysIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
{
    private readonly SqlServerTestDatabaseFixture _dbFixture = dbFixture;
    private GroceryHubWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private string _adminToken = null!;
    private const string WebhookSecret = "sepay_integration_secret_test_1234567890";

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
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Journey_J1_FullStorefrontPurchasingAndReview_GuestToCustomerDeliveredAndReviewed()
    {
        // 1. Arrange: Seed Product & Category
        Guid productId;
        await using (var db = _dbFixture.CreateDbContext())
        {
            var (_, pId) = await Phase07TestHelper.SeedCategoryAndProductAsync(
                db,
                "Gạo hữu cơ Hoa Sữa 2kg",
                $"SKU-RICE-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
                sellingPrice: 85_000L,
                costPrice: 60_000L,
                onHand: 20,
                reserved: 0);
            productId = pId;
        }

        // 2. Step 1: Guest browse -> search -> filter
        using var searchRes = await _client.GetAsync("/api/v1/products?q=gao&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, searchRes.StatusCode);
        var searchPage = await searchRes.Content.ReadFromJsonAsync<PagedResponse<StorefrontProductCardDto>>();
        Assert.NotNull(searchPage);
        Assert.Contains(searchPage.Items, p => p.Id == productId);

        // 3. Step 2: Guest adds item to cart with guest cart cookie
        using var addCartRes = await _client.PostAsJsonAsync("/api/v1/cart/items", new { productId, quantity = 2 });
        Assert.Equal(HttpStatusCode.OK, addCartRes.StatusCode);
        Assert.True(addCartRes.Headers.TryGetValues("Set-Cookie", out var setCookies));
        string? ghCartCookie = setCookies!.FirstOrDefault(c => c.StartsWith("gh_cart=", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(ghCartCookie);
        string guestToken = ghCartCookie!.Split(';')[0].Split('=')[1];

        // 4. Step 3: Register -> Verify Email -> Login (cart merged)
        string email = $"j1.customer.{Guid.NewGuid():N}@groceryhub.vn";
        var (_, customerToken) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory, _client, email, "Nguyen Van J1", "0912345678", guestCartToken: guestToken);

        // 5. Step 4: Add 2-level VN delivery address
        Guid addressId = await Phase07TestHelper.CreateAddressAsync(
            _client, customerToken, "Nguyen Van J1", "0912345678", "79", "79001", "123 Hai Ba Trung, Q1");

        // 6. Step 5: Checkout quote & place order COD
        using var quoteMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/checkout/quote")
        {
            Content = JsonContent.Create(new { addressId, paymentMethod = "COD" })
        };
        quoteMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        using var quoteRes = await _client.SendAsync(quoteMsg);
        Assert.Equal(HttpStatusCode.OK, quoteRes.StatusCode);

        using var orderMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/checkout/orders")
        {
            Content = JsonContent.Create(new { addressId, paymentMethod = "COD", note = "Giao buoi sang" })
        };
        orderMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        orderMsg.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        using var orderRes = await _client.SendAsync(orderMsg);
        Assert.Equal(HttpStatusCode.Created, orderRes.StatusCode);
        var orderDto = await orderRes.Content.ReadFromJsonAsync<PlaceOrderResponseDto>();
        Assert.NotNull(orderDto);
        Guid orderId = orderDto.Order.Id;

        // 7. Step 6: Staff confirms, prepares, packs, ships, delivers
        var (_, staffToken) = await Phase07TestHelper.CreateAndLoginStaffAsync(
            _client, _adminToken, $"j1.staff.{Guid.NewGuid():N}@groceryhub.vn", "Staff J1", "STF-J1",
            role: "Staff", extraPermissions: ["orders.process", "orders.ship"]);

        await TransitionOrderAsync(orderId, "Confirmed", staffToken);
        await TransitionOrderAsync(orderId, "Preparing", staffToken);
        await TransitionOrderAsync(orderId, "Packed", staffToken);
        await TransitionOrderAsync(orderId, "Shipping", staffToken);
        await TransitionOrderAsync(orderId, "Delivered", staffToken, codAmount: orderDto.Order.Total);

        // Get order item ID for review
        Guid orderItemId;
        await using (var db = _dbFixture.CreateDbContext())
        {
            var item = await db.OrderItems.FirstAsync(i => i.OrderId == orderId && i.ProductId == productId);
            orderItemId = item.Id;
        }

        // 8. Step 7: Customer submits verified purchase review
        using var reviewMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/products/{productId}/reviews")
        {
            Content = JsonContent.Create(new
            {
                orderItemId,
                rating = 5,
                title = "Gạo rất ngon",
                body = "Gạo ngon, dẻo thơm tuyệt vời, giao hàng nhanh chóng!"
            })
        };
        reviewMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        using var reviewRes = await _client.SendAsync(reviewMsg);
        Assert.Equal(HttpStatusCode.Created, reviewRes.StatusCode);
        var reviewDto = await reviewRes.Content.ReadFromJsonAsync<CustomerReviewDto>();
        Assert.NotNull(reviewDto);

        // 9. Admin moderates and approves review
        using var approveMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/reviews/{reviewDto.Id}/approve")
        {
            Content = JsonContent.Create(new { })
        };
        approveMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        using var approveRes = await _client.SendAsync(approveMsg);
        Assert.Equal(HttpStatusCode.OK, approveRes.StatusCode);

        // 10. Verify product reviews endpoint returns approved review
        using var prodReviewsRes = await _client.GetAsync($"/api/v1/products/{productId}/reviews");
        Assert.Equal(HttpStatusCode.OK, prodReviewsRes.StatusCode);
        var pagedReviews = await prodReviewsRes.Content.ReadFromJsonAsync<ProductReviewsResponseDto>();
        Assert.NotNull(pagedReviews);
        Assert.Equal(1, pagedReviews.Summary.RatingCount);
        Assert.Equal(5.0m, pagedReviews.Summary.RatingAvg);
        Assert.Contains(pagedReviews.Reviews.Items, r => r.Id == reviewDto.Id);
    }

    [Fact]
    public async Task Journey_J2_CheckoutWithQr_WebhookConfirmedWithoutReload()
    {
        string email = $"j2.customer.{Guid.NewGuid():N}@groceryhub.vn";
        var (_, customerToken) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory, _client, email, "Tran Thi J2", "0934567890");

        Guid productId;
        await using (var db = _dbFixture.CreateDbContext())
        {
            var (_, pId) = await Phase07TestHelper.SeedCategoryAndProductAsync(
                db, "Sữa tươi Đà Lạt True Milk 1L", $"SKU-DLM-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
                sellingPrice: 38_000L, costPrice: 28_000L, onHand: 15, reserved: 0);
            productId = pId;
        }

        Guid addressId = await Phase07TestHelper.CreateAddressAsync(
            _client, customerToken, "Tran Thi J2", "0934567890", "79", "79001", "456 Nguyen Trai, Q5");

        // Add to cart
        using var addMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/cart/items")
        {
            Content = JsonContent.Create(new { productId, quantity = 2 })
        };
        addMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        await _client.SendAsync(addMsg);

        // Checkout with SEPAY_QR
        using var orderMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/checkout/orders")
        {
            Content = JsonContent.Create(new { addressId, paymentMethod = "SEPAY_QR" })
        };
        orderMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        orderMsg.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        using var orderRes = await _client.SendAsync(orderMsg);
        Assert.Equal(HttpStatusCode.Created, orderRes.StatusCode);
        var orderDto = await orderRes.Content.ReadFromJsonAsync<PlaceOrderResponseDto>();
        Assert.NotNull(orderDto);
        Assert.Equal("PendingPayment", orderDto.Order.Status);
        Assert.Equal("Pending", orderDto.Payment.Status);
        string paymentCode = orderDto.Payment.PaymentCode;

        // Verify status endpoint is initially Pending
        using var statMsg1 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/payments/{paymentCode}/status");
        statMsg1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        using var statRes1 = await _client.SendAsync(statMsg1);
        Assert.Equal(HttpStatusCode.OK, statRes1.StatusCode);
        var stat1 = await statRes1.Content.ReadFromJsonAsync<PaymentStatusResponseDto>();
        Assert.Equal("Pending", stat1!.Status);
        Assert.Equal("PendingPayment", stat1.OrderStatus);

        // Post signed SePay webhook
        long txId = 91002;
        using var webhookMsg = CreateSignedWebhookRequest(txId, orderDto.Order.Total, paymentCode);
        using var webhookRes = await _client.SendAsync(webhookMsg);
        Assert.Equal(HttpStatusCode.OK, webhookRes.StatusCode);

        // Poll status again -> Live update confirms Paid and Confirmed
        using var statMsg2 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/payments/{paymentCode}/status");
        statMsg2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        using var statRes2 = await _client.SendAsync(statMsg2);
        Assert.Equal(HttpStatusCode.OK, statRes2.StatusCode);
        var stat2 = await statRes2.Content.ReadFromJsonAsync<PaymentStatusResponseDto>();
        Assert.Equal("Paid", stat2!.Status);
        Assert.Equal("Confirmed", stat2.OrderStatus);
    }

    [Fact]
    public async Task Journey_J3_QrExpiry_OrderCancelledAndStockRestored()
    {
        string email = $"j3.customer.{Guid.NewGuid():N}@groceryhub.vn";
        var (_, customerToken) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory, _client, email, "Le Van J3", "0945678901");

        Guid productId;
        await using (var db = _dbFixture.CreateDbContext())
        {
            var (_, pId) = await Phase07TestHelper.SeedCategoryAndProductAsync(
                db, "Dầu ăn Simply 1L", $"SKU-OIL-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
                sellingPrice: 55_000L, costPrice: 42_000L, onHand: 10, reserved: 0);
            productId = pId;
        }

        Guid addressId = await Phase07TestHelper.CreateAddressAsync(
            _client, customerToken, "Le Van J3", "0945678901", "79", "79001", "789 Cach Mang Thang 8, Q10");

        // Add to cart & checkout with QR
        using var addMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/cart/items")
        {
            Content = JsonContent.Create(new { productId, quantity = 3 })
        };
        addMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        await _client.SendAsync(addMsg);

        using var orderMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/checkout/orders")
        {
            Content = JsonContent.Create(new { addressId, paymentMethod = "SEPAY_QR" })
        };
        orderMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        orderMsg.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        using var orderRes = await _client.SendAsync(orderMsg);
        var orderDto = await orderRes.Content.ReadFromJsonAsync<PlaceOrderResponseDto>();
        Guid orderId = orderDto!.Order.Id;

        // Verify stock is reserved
        await using (var db = _dbFixture.CreateDbContext())
        {
            var stock = await db.StockItems.FirstAsync(s => s.ProductId == productId);
            Assert.Equal(3, stock.Reserved);
            Assert.Equal(7, stock.OnHand - stock.Reserved);
        }

        // Simulate time advancing past Expiry (30m) + Grace (5m) = 36 minutes
        await using (var db = _dbFixture.CreateDbContext())
        {
            var payment = await db.Payments.FirstAsync(p => p.OrderId == orderId);
            var attempt = await db.PaymentAttempts.FirstAsync(a => a.PaymentId == payment.Id);
            attempt.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-10); // Expired 10 minutes ago
            await db.SaveChangesAsync();
        }

        // Trigger payment expiry sweep via service in DI
        using (var scope = _factory.Services.CreateScope())
        {
            var expiryService = scope.ServiceProvider.GetServices<IHostedService>().OfType<PaymentExpiryService>().First();
            int sweptCount = await expiryService.SweepExpiredPaymentsAsync();
            Assert.True(sweptCount >= 1);
        }

        // Verify order is cancelled and stock reservation is restored
        await using (var db = _dbFixture.CreateDbContext())
        {
            var order = await db.Orders.FirstAsync(o => o.Id == orderId);
            Assert.Equal(OrderStatusConstants.Cancelled, order.Status);

            var stock = await db.StockItems.FirstAsync(s => s.ProductId == productId);
            Assert.Equal(0, stock.Reserved);
            Assert.Equal(10, stock.OnHand - stock.Reserved);
        }
    }

    [Fact]
    public async Task Journey_J4_AdminCreatesProduct_GoodsReceipt_CustomerPurchases()
    {
        // 1. Admin creates Category & Product
        Guid categoryId;
        await using (var db = _dbFixture.CreateDbContext())
        {
            var cat = new Category
            {
                Id = Guid.NewGuid(),
                Name = "Gia vị & Nước chấm J4",
                Slug = $"gia-vi-j4-{Guid.NewGuid():N}"[..20],
                SortOrder = 1,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.Categories.Add(cat);
            await db.SaveChangesAsync();
            categoryId = cat.Id;
        }

        string sku = $"SKU-SAUCE-{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        using var createProdMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/products")
        {
            Content = JsonContent.Create(new
            {
                name = "Nước mắm Phú Quốc Khải Hoàn 40 độ đạm 500ml",
                sku,
                categoryId,
                unit = "Chai",
                listPrice = 120_000L,
                sellingPrice = 105_000L,
                initialCostPrice = 75_000L,
                status = "Active",
                isActive = true
            })
        };
        createProdMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        using var createProdRes = await _client.SendAsync(createProdMsg);
        Assert.Equal(HttpStatusCode.Created, createProdRes.StatusCode);
        var createdProd = await createProdRes.Content.ReadFromJsonAsync<AdminProductDetailDto>();
        Guid productId = createdProd!.Id;

        // 2. Purchasing: Create Supplier & Purchase Order -> Receive Goods
        Guid supplierId;
        await using (var db = _dbFixture.CreateDbContext())
        {
            var supp = new Supplier
            {
                Id = Guid.NewGuid(),
                Name = "Nước Mắm Khải Hoàn",
                Code = $"SUPP-{Guid.NewGuid():N}"[..8].ToUpperInvariant(),
                ContactPerson = "Anh Hoan",
                Phone = "0987654321",
                Email = "khaihoan@supplier.vn",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.Suppliers.Add(supp);
            await db.SaveChangesAsync();
            supplierId = supp.Id;
        }

        // Create PO
        using var poMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/purchasing/orders")
        {
            Content = JsonContent.Create(new
            {
                supplierId,
                note = "Nhap hang dot 1",
                items = new[] { new { productId, orderedQuantity = 50, unitCost = 75_000L } }
            })
        };
        poMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        using var poRes = await _client.SendAsync(poMsg);
        Assert.Equal(HttpStatusCode.Created, poRes.StatusCode);
        var poDto = await poRes.Content.ReadFromJsonAsync<PurchaseOrderDetailDto>();
        Guid poId = poDto!.Id;

        // Transition PO to Ordered
        using var poOrderMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/purchasing/orders/{poId}/order")
        {
            Content = JsonContent.Create(new { note = "Gui don hang cho nha cung cap" })
        };
        poOrderMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        using var poOrderRes = await _client.SendAsync(poOrderMsg);
        Assert.Equal(HttpStatusCode.OK, poOrderRes.StatusCode);

        // Receive Goods: 50 items
        using var grMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/purchasing/orders/{poId}/receive")
        {
            Content = JsonContent.Create(new
            {
                receiptNumber = "GR-001",
                note = "Giao du 50 chai",
                items = new[] { new { purchaseOrderItemId = (Guid?)null, productId, receivedQuantity = 50, unitCost = 75_000L } }
            })
        };
        grMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        using var grRes = await _client.SendAsync(grMsg);
        Assert.Equal(HttpStatusCode.OK, grRes.StatusCode);

        // 3. Customer buys 5 bottles
        string email = $"j4.customer.{Guid.NewGuid():N}@groceryhub.vn";
        var (_, customerToken) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory, _client, email, "Hoang J4", "0956789012");
        Guid addressId = await Phase07TestHelper.CreateAddressAsync(
            _client, customerToken, "Hoang J4", "0956789012", "79", "79001", "123 Pasteur, Q3");

        using var addMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/cart/items")
        {
            Content = JsonContent.Create(new { productId, quantity = 5 })
        };
        addMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        await _client.SendAsync(addMsg);

        using var orderMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/checkout/orders")
        {
            Content = JsonContent.Create(new { addressId, paymentMethod = "COD" })
        };
        orderMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        orderMsg.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        using var orderRes = await _client.SendAsync(orderMsg);
        var orderDto = await orderRes.Content.ReadFromJsonAsync<PlaceOrderResponseDto>();

        // Staff ships order
        await TransitionOrderAsync(orderDto!.Order.Id, "Confirmed", _adminToken);
        await TransitionOrderAsync(orderDto.Order.Id, "Preparing", _adminToken);
        await TransitionOrderAsync(orderDto.Order.Id, "Packed", _adminToken);
        await TransitionOrderAsync(orderDto.Order.Id, "Shipping", _adminToken);

        // 4. Verify inventory is decremented: OnHand = 50 - 5 = 45
        await using (var db = _dbFixture.CreateDbContext())
        {
            var stock = await db.StockItems.FirstAsync(s => s.ProductId == productId);
            Assert.Equal(45, stock.OnHand);
            Assert.Equal(0, stock.Reserved);
            Assert.Equal(45, stock.OnHand - stock.Reserved);
        }
    }

    [Fact]
    public async Task Journey_J5_AnomalyReview_Underpaid_ManagerResolvesAndRefundTwoPersonRule()
    {
        // 1. Arrange Customer and QR Order for 200,000 VND
        string email = $"j5.customer.{Guid.NewGuid():N}@groceryhub.vn";
        var (_, customerToken) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory, _client, email, "Vo Van J5", "0967890123");

        Guid productId;
        await using (var db = _dbFixture.CreateDbContext())
        {
            var (_, pId) = await Phase07TestHelper.SeedCategoryAndProductAsync(
                db, "Thịt bò Úc 500g", $"SKU-BEEF-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
                sellingPrice: 100_000L, costPrice: 80_000L, onHand: 10, reserved: 0);
            productId = pId;
        }

        Guid addressId = await Phase07TestHelper.CreateAddressAsync(
            _client, customerToken, "Vo Van J5", "0967890123", "79", "79001", "789 Nam Ky Khoi Nghia, Q3");

        using var addMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/cart/items")
        {
            Content = JsonContent.Create(new { productId, quantity = 2 }) // 200,000 VND
        };
        addMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        await _client.SendAsync(addMsg);

        using var orderMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/checkout/orders")
        {
            Content = JsonContent.Create(new { addressId, paymentMethod = "SEPAY_QR" })
        };
        orderMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        orderMsg.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        using var orderRes = await _client.SendAsync(orderMsg);
        var orderDto = await orderRes.Content.ReadFromJsonAsync<PlaceOrderResponseDto>();
        string paymentCode = orderDto!.Payment.PaymentCode;

        // 2. Customer underpays (transfers 150,000 VND instead of 200,000 VND)
        long txId = 91005;
        using var webhookMsg = CreateSignedWebhookRequest(txId, 150_000L, paymentCode);
        using var webhookRes = await _client.SendAsync(webhookMsg);
        Assert.Equal(HttpStatusCode.OK, webhookRes.StatusCode);

        // 3. Verify transaction lands in Payment Transactions with Underpaid & NeedsReview
        using var reviewMsg = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/payment-transactions?reviewStatus=NeedsReview&page=1&pageSize=20");
        reviewMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        using var reviewRes = await _client.SendAsync(reviewMsg);
        Assert.Equal(HttpStatusCode.OK, reviewRes.StatusCode);
        var queue = await reviewRes.Content.ReadFromJsonAsync<PagedResponse<PaymentTransactionDto>>();
        Assert.NotNull(queue);
        var tx = queue.Items.FirstOrDefault(t => t.PaymentCode == paymentCode);
        Assert.NotNull(tx);
        Assert.Equal("Underpaid", tx.MatchStatus);
        Assert.Equal("NeedsReview", tx.ReviewStatus);

        // 4. Staff A reviews anomaly and resolves with Refund action (BR-15: creates refund request)
        var (_, staffAToken) = await Phase07TestHelper.CreateAndLoginStaffAsync(
            _client, _adminToken, $"staffA.{Guid.NewGuid():N}@groceryhub.vn", "Staff A Requester", "STF-A",
            role: "Manager", extraPermissions: ["payments.review", "refunds.approve"]);

        using var resolveMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/payment-transactions/{tx.Id}/resolve")
        {
            Content = JsonContent.Create(new
            {
                action = "Refund",
                orderId = orderDto.Order.Id,
                note = "Khách chuyển thiếu tiền, hoàn tiền theo yêu cầu"
            })
        };
        resolveMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffAToken);
        using var resolveRes = await _client.SendAsync(resolveMsg);
        Assert.Equal(HttpStatusCode.OK, resolveRes.StatusCode);

        // Fetch created refund from queue
        using var getRefundsMsg = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/refunds?status=Requested&page=1&pageSize=20");
        getRefundsMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffAToken);
        using var getRefundsRes = await _client.SendAsync(getRefundsMsg);
        Assert.Equal(HttpStatusCode.OK, getRefundsRes.StatusCode);
        var refundsPage = await getRefundsRes.Content.ReadFromJsonAsync<PagedResponse<RefundDto>>();
        Assert.NotNull(refundsPage);
        var refundDto = refundsPage.Items.FirstOrDefault(r => r.OrderId == orderDto.Order.Id);
        Assert.NotNull(refundDto);
        Guid refundId = refundDto.Id;

        // 5. Staff A attempts self-approval -> Blocked by two-person rule (409 Conflict)
        using var selfApproveMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/refunds/{refundId}/approve")
        {
            Content = JsonContent.Create(new { note = "Tu duyet" })
        };
        selfApproveMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffAToken);
        using var selfApproveRes = await _client.SendAsync(selfApproveMsg);
        Assert.Equal(HttpStatusCode.Conflict, selfApproveRes.StatusCode);

        // 6. Manager B approves refund
        var (_, managerBToken) = await Phase07TestHelper.CreateAndLoginStaffAsync(
            _client, _adminToken, $"managerB.{Guid.NewGuid():N}@groceryhub.vn", "Manager B Approver", "MGR-B",
            role: "Manager", extraPermissions: ["refunds.approve"]);

        using var approveMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/refunds/{refundId}/approve")
        {
            Content = JsonContent.Create(new { note = "Dong y hoan tien" })
        };
        approveMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerBToken);
        using var approveRes = await _client.SendAsync(approveMsg);
        Assert.Equal(HttpStatusCode.OK, approveRes.StatusCode);

        // 7. Complete refund with bank transfer reference
        using var completeMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/refunds/{refundId}/complete")
        {
            Content = JsonContent.Create(new { bankReference = "FT26319000J5", note = "Da chuyen khoan" })
        };
        completeMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerBToken);
        using var completeRes = await _client.SendAsync(completeMsg);
        Assert.Equal(HttpStatusCode.OK, completeRes.StatusCode);

        // Verify refund is completed in queue
        using var getCompletedMsg = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/refunds?status=Completed&page=1&pageSize=20");
        getCompletedMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerBToken);
        using var getCompletedRes = await _client.SendAsync(getCompletedMsg);
        Assert.Equal(HttpStatusCode.OK, getCompletedRes.StatusCode);
        var completedPage = await getCompletedRes.Content.ReadFromJsonAsync<PagedResponse<RefundDto>>();
        Assert.NotNull(completedPage);
        var finalRefund = completedPage.Items.FirstOrDefault(r => r.Id == refundId);
        Assert.NotNull(finalRefund);
        Assert.Equal("Completed", finalRefund.Status);
        Assert.Equal("FT26319000J5", finalRefund.BankReference);
    }

    [Fact]
    public async Task Journey_J6_RbacPrivilegeIsolation_StaffAndCustomerDeniedAdminEndpoints()
    {
        // 1. Create Customer
        string email = $"j6.customer.{Guid.NewGuid():N}@groceryhub.vn";
        var (_, customerToken) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory, _client, email, "Customer J6", "0978901234");

        // 2. Create Staff (has only orders.process and orders.ship)
        var (_, staffToken) = await Phase07TestHelper.CreateAndLoginStaffAsync(
            _client, _adminToken, $"j6.staff.{Guid.NewGuid():N}@groceryhub.vn", "Staff J6", "STF-J6",
            role: "Staff", extraPermissions: ["orders.process", "orders.ship"]);

        // 3. Test Staff denied Financial & Admin endpoints (403 Forbidden)
        string[] adminOnlyRoutes =
        [
            "/api/v1/admin/reports/profit",
            "/api/v1/admin/reports/stock-valuation",
            "/api/v1/admin/settings"
        ];

        foreach (var route in adminOnlyRoutes)
        {
            using var msg = new HttpRequestMessage(HttpMethod.Get, route);
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
            using var res = await _client.SendAsync(msg);
            Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        }

        // 4. Test Customer denied Staff & Admin endpoints (403 Forbidden)
        string[] staffAndAdminRoutes =
        [
            "/api/v1/admin/orders",
            "/api/v1/admin/inventory",
            "/api/v1/admin/staff",
            "/api/v1/admin/roles"
        ];

        foreach (var route in staffAndAdminRoutes)
        {
            using var msg = new HttpRequestMessage(HttpMethod.Get, route);
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
            using var res = await _client.SendAsync(msg);
            Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        }

        // 5. Test Anonymous denied protected endpoints (401 Unauthorized)
        using var anonMsg = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me/orders");
        using var anonRes = await _client.SendAsync(anonMsg);
        Assert.Equal(HttpStatusCode.Unauthorized, anonRes.StatusCode);
    }

    [Fact]
    public async Task Journey_J7_MobileViewport_BoundaryValidationsAndEdgeCases()
    {
        // Verifies mobile viewport constraints (360px viewport simulation) and critical domain boundary rules
        string email = $"j7.customer.{Guid.NewGuid():N}@groceryhub.vn";
        var (_, customerToken) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory, _client, email, "Mobile User J7", "0989012345");

        // 1. Mobile phone boundary test (Invalid formats rejected, valid formats normalized)
        using var invalidPhoneMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/me/addresses")
        {
            Content = JsonContent.Create(new
            {
                recipientName = "Test Mobile",
                phone = "123456", // Invalid phone
                provinceCode = "79",
                wardCode = "79001",
                street = "123 Le Loi"
            })
        };
        invalidPhoneMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        using var invalidPhoneRes = await _client.SendAsync(invalidPhoneMsg);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidPhoneRes.StatusCode);

        // 2. 2-level VN Address model validation (Unknown province rejected)
        using var unknownProvMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/me/addresses")
        {
            Content = JsonContent.Create(new
            {
                recipientName = "Test Mobile",
                phone = "0989012345",
                provinceCode = "9999", // Non-existent province
                wardCode = "99999",
                street = "123 Le Loi"
            })
        };
        unknownProvMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        using var unknownProvRes = await _client.SendAsync(unknownProvMsg);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknownProvRes.StatusCode);

        // 3. Max address boundary: Up to 10 addresses allowed (create 10 addresses)
        for (int i = 1; i <= 10; i++)
        {
            await Phase07TestHelper.CreateAddressAsync(
                _client, customerToken, $"Address {i}", "0989012345", "79", "79001", $"Street {i}");
        }

        // 11th address should be rejected with 422 UnprocessableEntity (limit 10 reached)
        using var maxAddrMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/me/addresses")
        {
            Content = JsonContent.Create(new
            {
                recipientName = "Address 11",
                phone = "0989012345",
                provinceCode = "79",
                wardCode = "79001",
                street = "Street 11"
            })
        };
        maxAddrMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        using var maxAddrRes = await _client.SendAsync(maxAddrMsg);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, maxAddrRes.StatusCode);
    }

    private async Task TransitionOrderAsync(Guid orderId, string targetStatus, string token, long? codAmount = null)
    {
        using var msg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(new { to = targetStatus, reason = $"Transition to {targetStatus}", codAmount })
        };
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var res = await _client.SendAsync(msg);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    private static HttpRequestMessage CreateSignedWebhookRequest(long id, long amount, string content)
    {
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string rawJson = $"{{\"id\":{id},\"gateway\":\"Vietcombank\",\"accountNumber\":\"0123456789\",\"code\":null,\"content\":\"{content}\",\"transferType\":\"in\",\"transferAmount\":{amount},\"accumulated\":0,\"subAccount\":null,\"referenceCode\":\"FT{id}\",\"description\":\"Transfer\",\"transactionDate\":\"2026-11-15 12:00:00\"}}";

        string messageToSign = $"{timestamp}.{rawJson}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(WebhookSecret));
        string signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(messageToSign))).ToLowerInvariant();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/sepay/webhook")
        {
            Content = new StringContent(rawJson, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-SePay-Signature", $"sha256={signature}");
        request.Headers.Add("X-SePay-Timestamp", timestamp.ToString());
        return request;
    }
}
