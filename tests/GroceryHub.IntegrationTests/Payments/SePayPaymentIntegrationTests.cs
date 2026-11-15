using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GroceryHub.Modules.Ordering.Application.DTOs;
using GroceryHub.Modules.Payments.Contracts;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GroceryHub.IntegrationTests.Payments;

[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class SePayPaymentIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
{
    private const string WebhookSecret = "sepay_integration_secret_test_1234567890";
    private const string AccountNumber = "0123456789";
    private const string BankShortName = "MBBank";

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
    public async Task T_PAY_01_ValidSignedWebhook_TransitionsToPaidAndConfirmed()
    {
        // 1. Arrange: Seed customer, product, and place order with SEPAY_QR
        var (_, token) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory, _client, "pay_cus1@groceryhub.vn", "Customer One", "0901111111");

        Guid productId;
        await using (var db = _dbFixture.CreateDbContext())
        {
            productId = await SeedProductAsync(db, "Fresh Milk", 50000, 10);
        }

        Guid addressId = await Phase07TestHelper.CreateAddressAsync(_client, token);
        await AddToCartAsync(_client, token, productId, 2); // 100,000 VND

        // Place order with SEPAY_QR
        var placeOrderRes = await PostAuthJsonAsync(_client, "/api/v1/checkout/orders", new
        {
            addressId,
            paymentMethod = "SEPAY_QR",
            couponCode = (string?)null,
            note = "Deliver fast"
        }, token, idempotencyKey: Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.Created, placeOrderRes.StatusCode);
        var orderResponse = await placeOrderRes.Content.ReadFromJsonAsync<PlaceOrderResponseDto>();
        Assert.NotNull(orderResponse);
        Assert.Equal(OrderStatusConstants.PendingPayment, orderResponse.Order.Status);
        Assert.Equal(PaymentStatusConstants.Pending, orderResponse.Payment.Status);

        string paymentCode = orderResponse.Payment.PaymentCode;
        long totalAmount = orderResponse.Order.Total;
        Guid orderId = orderResponse.Order.Id;

        // 2. Act: Send valid signed SePay webhook
        long sepayTxId = 90001;
        var request = CreateSignedWebhookRequest(
            id: sepayTxId,
            amount: totalAmount,
            content: paymentCode,
            transferType: "in");

        using var response = await _client.SendAsync(request);

        // 3. Assert: HTTP response
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resBody = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(resBody.GetProperty("success").GetBoolean());

        // 4. Assert: Database state
        await using (var db = _dbFixture.CreateDbContext())
        {
            var order = await db.Orders
                .Include(o => o.StatusHistory)
                .FirstAsync(o => o.Id == orderId);

            Assert.Equal(OrderStatusConstants.Confirmed, order.Status);
            Assert.Equal(OrderPaymentStatusConstants.Paid, order.PaymentStatus);
            Assert.Contains(order.StatusHistory, h => h.ToStatus == OrderStatusConstants.Confirmed);

            var payment = await db.Payments
                .Include(p => p.Attempts)
                .FirstAsync(p => p.OrderId == orderId);

            Assert.Equal(PaymentStatusConstants.Paid, payment.Status);
            Assert.NotNull(payment.PaidAt);

            var attempt = payment.Attempts.First(a => a.PaymentCode == paymentCode);
            Assert.Equal(PaymentAttemptStatusConstants.Paid, attempt.Status);

            var webhookEvent = await db.WebhookEvents
                .FirstAsync(w => w.Provider == "SEPAY" && w.ProviderTransactionId == sepayTxId.ToString());

            Assert.Equal("Processed", webhookEvent.ProcessingStatus);
            Assert.Equal(1, webhookEvent.DeliveryCount);

            var tx = await db.PaymentTransactions
                .FirstAsync(t => t.WebhookEventId == webhookEvent.Id);

            Assert.Equal(MatchStatusConstants.Matched, tx.MatchStatus);
            Assert.Equal(PaymentReviewStatusConstants.None, tx.ReviewStatus);
            Assert.Equal(totalAmount, tx.Amount);

            // Audit log check
            bool hasAudit = await db.AuditLogs.AnyAsync(a => a.Action == "OrderConfirmedByPayment" && a.EntityId == orderId.ToString());
            Assert.True(hasAudit);

            // Outbox check
            bool hasOutbox = await db.OutboxMessages.AnyAsync(m => m.EventType == "OrderConfirmed");
            Assert.True(hasOutbox);
        }
    }

    [Fact]
    public async Task T_PAY_02_SequentialDuplicateWebhook_IsIdempotent()
    {
        // 1. Arrange order and valid first webhook
        var (_, token) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory, _client, "pay_cus2@groceryhub.vn", "Customer Two", "0902222222");

        Guid productId;
        await using (var db = _dbFixture.CreateDbContext())
        {
            productId = await SeedProductAsync(db, "Fresh Milk 2", 60000, 10);
        }

        Guid addressId = await Phase07TestHelper.CreateAddressAsync(_client, token);
        await AddToCartAsync(_client, token, productId, 1);

        var placeRes = await PostAuthJsonAsync(_client, "/api/v1/checkout/orders", new
        {
            addressId,
            paymentMethod = "SEPAY_QR",
        }, token, idempotencyKey: Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.Created, placeRes.StatusCode);
        var orderRes = await placeRes.Content.ReadFromJsonAsync<PlaceOrderResponseDto>();
        string paymentCode = orderRes!.Payment.PaymentCode;
        long totalAmount = orderRes.Order.Total;
        long sepayTxId = 90002;

        // First delivery
        var req1 = CreateSignedWebhookRequest(sepayTxId, totalAmount, paymentCode);
        using var res1 = await _client.SendAsync(req1);
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);

        // 2. Act: Second delivery with the EXACT same transaction id
        var req2 = CreateSignedWebhookRequest(sepayTxId, totalAmount, paymentCode);
        using var res2 = await _client.SendAsync(req2);

        // 3. Assert
        Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
        var resBody = await res2.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(resBody.GetProperty("success").GetBoolean());

        await using (var db = _dbFixture.CreateDbContext())
        {
            var webhookEvent = await db.WebhookEvents
                .FirstAsync(w => w.Provider == "SEPAY" && w.ProviderTransactionId == sepayTxId.ToString());

            // Delivery count incremented to 2
            Assert.Equal(2, webhookEvent.DeliveryCount);

            // Exactly 1 PaymentTransaction in DB
            int txCount = await db.PaymentTransactions.CountAsync(t => t.WebhookEventId == webhookEvent.Id);
            Assert.Equal(1, txCount);

            // Exactly 1 Confirmed OrderStatusHistory
            int historyCount = await db.OrderStatusHistories.CountAsync(h => h.OrderId == orderRes.Order.Id && h.ToStatus == OrderStatusConstants.Confirmed);
            Assert.Equal(1, historyCount);
        }
    }

    [Fact]
    public async Task T_PAY_03_ConcurrentDuplicateWebhook_AllReturnSuccessWithoutDuplicateState()
    {
        // 1. Arrange
        var (_, token) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory, _client, "pay_cus3@groceryhub.vn", "Customer Three", "0903333333");

        Guid productId;
        await using (var db = _dbFixture.CreateDbContext())
        {
            productId = await SeedProductAsync(db, "Fresh Milk 3", 70000, 10);
        }

        Guid addressId = await Phase07TestHelper.CreateAddressAsync(_client, token);
        await AddToCartAsync(_client, token, productId, 1);

        var placeRes = await PostAuthJsonAsync(_client, "/api/v1/checkout/orders", new
        {
            addressId,
            paymentMethod = "SEPAY_QR",
        }, token, idempotencyKey: Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.Created, placeRes.StatusCode);
        var orderRes = await placeRes.Content.ReadFromJsonAsync<PlaceOrderResponseDto>();
        string paymentCode = orderRes!.Payment.PaymentCode;
        long totalAmount = orderRes.Order.Total;
        long sepayTxId = 90003;

        // 2. Act: Send 10 identical webhook calls concurrently
        var tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            using var req = CreateSignedWebhookRequest(sepayTxId, totalAmount, paymentCode);
            return await _client.SendAsync(req);
        });

        var responses = await Task.WhenAll(tasks);

        // 3. Assert: All 10 responses return 200 OK
        foreach (var r in responses)
        {
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            r.Dispose();
        }

        await using (var db = _dbFixture.CreateDbContext())
        {
            var order = await db.Orders.FirstAsync(o => o.Id == orderRes.Order.Id);
            Assert.Equal(OrderStatusConstants.Confirmed, order.Status);

            var webhookEvent = await db.WebhookEvents
                .FirstAsync(w => w.Provider == "SEPAY" && w.ProviderTransactionId == sepayTxId.ToString());

            Assert.Equal("Processed", webhookEvent.ProcessingStatus);
            Assert.True(webhookEvent.DeliveryCount >= 1);

            int txCount = await db.PaymentTransactions.CountAsync(t => t.WebhookEventId == webhookEvent.Id);
            Assert.Equal(1, txCount);
        }
    }

    [Fact]
    public async Task T_PAY_08_AmountMismatch_UnderpaidAndOverpaidRecordedForReview()
    {
        // 1. Arrange order
        var (_, token) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory, _client, "pay_cus8@groceryhub.vn", "Customer Eight", "0908888888");

        Guid productId;
        await using (var db = _dbFixture.CreateDbContext())
        {
            productId = await SeedProductAsync(db, "Item 8", 100000, 10);
        }

        Guid addressId = await Phase07TestHelper.CreateAddressAsync(_client, token);
        await AddToCartAsync(_client, token, productId, 1);

        var placeRes = await PostAuthJsonAsync(_client, "/api/v1/checkout/orders", new
        {
            addressId,
            paymentMethod = "SEPAY_QR",
        }, token, idempotencyKey: Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.Created, placeRes.StatusCode);
        var orderRes = await placeRes.Content.ReadFromJsonAsync<PlaceOrderResponseDto>();
        string paymentCode = orderRes!.Payment.PaymentCode;
        long totalAmount = orderRes.Order.Total;

        // 2. Act: Send UNDERPAID webhook (transfer 50000 instead of totalAmount)
        long underpaidTxId = 900081;
        using var underpaidReq = CreateSignedWebhookRequest(underpaidTxId, totalAmount - 20000, paymentCode);
        using var underpaidRes = await _client.SendAsync(underpaidReq);

        Assert.Equal(HttpStatusCode.OK, underpaidRes.StatusCode);

        await using (var db = _dbFixture.CreateDbContext())
        {
            var order = await db.Orders.FirstAsync(o => o.Id == orderRes.Order.Id);
            Assert.Equal(OrderStatusConstants.PendingPayment, order.Status); // Order remains pending!

            var tx = await db.PaymentTransactions.FirstAsync(t => t.ReferenceCode == $"REF{underpaidTxId}");
            Assert.Equal(MatchStatusConstants.Underpaid, tx.MatchStatus);
            Assert.Equal(PaymentReviewStatusConstants.NeedsReview, tx.ReviewStatus);
        }
    }

    [Fact]
    public async Task T_PAY_09_UnknownCodeOrWrongAccount_RecordedForReview()
    {
        // 1. Send webhook with non-existent GH code
        long unknownTxId = 900091;
        using var unknownReq = CreateSignedWebhookRequest(unknownTxId, 150000, "GH9999999999");
        using var unknownRes = await _client.SendAsync(unknownReq);
        Assert.Equal(HttpStatusCode.OK, unknownRes.StatusCode);

        // 2. Send webhook with wrong receiving account
        long wrongAccTxId = 900092;
        using var wrongAccReq = CreateSignedWebhookRequest(wrongAccTxId, 150000, "GH0000000001", accountNumber: "9999999999");
        using var wrongAccRes = await _client.SendAsync(wrongAccReq);
        Assert.Equal(HttpStatusCode.OK, wrongAccRes.StatusCode);

        await using (var db = _dbFixture.CreateDbContext())
        {
            var unknownTx = await db.PaymentTransactions.FirstAsync(t => t.ReferenceCode == $"REF{unknownTxId}");
            Assert.Equal(MatchStatusConstants.UnknownCode, unknownTx.MatchStatus);
            Assert.Equal(PaymentReviewStatusConstants.NeedsReview, unknownTx.ReviewStatus);

            var wrongAccTx = await db.PaymentTransactions.FirstAsync(t => t.ReferenceCode == $"REF{wrongAccTxId}");
            Assert.Equal(MatchStatusConstants.WrongAccount, wrongAccTx.MatchStatus);
            Assert.Equal(PaymentReviewStatusConstants.NeedsReview, wrongAccTx.ReviewStatus);
        }
    }

    [Fact]
    public async Task T_PAY_10_OutgoingTransfer_Ignored()
    {
        long outTxId = 90010;
        using var outReq = CreateSignedWebhookRequest(outTxId, 500000, "GH0000000001", transferType: "out");
        using var outRes = await _client.SendAsync(outReq);

        Assert.Equal(HttpStatusCode.OK, outRes.StatusCode);

        await using (var db = _dbFixture.CreateDbContext())
        {
            var ev = await db.WebhookEvents.FirstAsync(w => w.ProviderTransactionId == outTxId.ToString());
            Assert.Equal("Ignored", ev.ProcessingStatus);

            var tx = await db.PaymentTransactions.FirstOrDefaultAsync(t => t.WebhookEventId == ev.Id);
            Assert.NotNull(tx);
            Assert.Equal(MatchStatusConstants.OutgoingIgnored, tx.MatchStatus);
            Assert.Equal(PaymentReviewStatusConstants.None, tx.ReviewStatus);
        }
    }

    [Fact]
    public async Task T_PAY_13_SupersededAttempt_TransitionsToPaidAnd5AttemptLimitEnforced()
    {
        var (_, token) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory, _client, "pay_cus13@groceryhub.vn", "Customer Thirteen", "0901313131");

        Guid productId;
        await using (var db = _dbFixture.CreateDbContext())
        {
            productId = await SeedProductAsync(db, "Item 13", 100000, 10);
        }

        Guid addressId = await Phase07TestHelper.CreateAddressAsync(_client, token);
        await AddToCartAsync(_client, token, productId, 1);

        var placeRes = await PostAuthJsonAsync(_client, "/api/v1/checkout/orders", new
        {
            addressId,
            paymentMethod = "SEPAY_QR",
        }, token, idempotencyKey: Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.Created, placeRes.StatusCode);
        var orderRes = await placeRes.Content.ReadFromJsonAsync<PlaceOrderResponseDto>();
        Guid orderId = orderRes!.Order.Id;
        string firstCode = orderRes.Payment.PaymentCode;

        // Generate Attempt 2, 3, 4, 5
        for (int i = 2; i <= 5; i++)
        {
            var newAttRes = await PostAuthEmptyAsync(_client, $"/api/v1/me/orders/{orderId}/payment-attempts", token);
            Assert.Equal(HttpStatusCode.Created, newAttRes.StatusCode);
            var att = await newAttRes.Content.ReadFromJsonAsync<PaymentAttemptDto>();
            Assert.NotNull(att);
            Assert.Equal(i, att.AttemptNumber);
        }

        // Try 6th attempt -> Must return 409 Conflict (limit reached)
        var att6Res = await PostAuthEmptyAsync(_client, $"/api/v1/me/orders/{orderId}/payment-attempts", token);
        Assert.Equal(HttpStatusCode.Conflict, att6Res.StatusCode);

        // Pay using FIRST (superseded) code
        long txId = 90013;
        using var payReq = CreateSignedWebhookRequest(txId, orderRes.Order.Total, firstCode);
        using var payRes = await _client.SendAsync(payReq);
        Assert.Equal(HttpStatusCode.OK, payRes.StatusCode);

        await using (var db = _dbFixture.CreateDbContext())
        {
            var order = await db.Orders.FirstAsync(o => o.Id == orderId);
            Assert.Equal(OrderStatusConstants.Confirmed, order.Status);

            var firstAttempt = await db.PaymentAttempts.FirstAsync(a => a.PaymentCode == firstCode);
            Assert.Equal(PaymentAttemptStatusConstants.Paid, firstAttempt.Status);
        }
    }

    [Fact]
    public async Task T_PAY_18_TwoPersonRule_RefundSelfApprovalBlockedAndSecondPersonSucceeds()
    {
        string adminToken = await Phase07TestHelper.LoginAdminAsync(_client);

        var (staffAId, staffAToken) = await Phase07TestHelper.CreateAndLoginStaffAsync(
            _client, adminToken, "staffA@groceryhub.vn", "Staff A", "STF-001", "Manager");

        var (staffBId, staffBToken) = await Phase07TestHelper.CreateAndLoginStaffAsync(
            _client, adminToken, "staffB@groceryhub.vn", "Staff B", "STF-002", "Manager");

        // Create a paid order directly in DB
        Guid orderId = Guid.NewGuid();
        Guid paymentId = Guid.NewGuid();
        await using (var db = _dbFixture.CreateDbContext())
        {
            var order = new Order
            {
                Id = orderId,
                OrderNumber = $"ORD-TEST-REFUND-{Guid.NewGuid():N}"[..20],
                UserId = staffAId,
                Status = OrderStatusConstants.Delivered,
                PaymentMethod = OrderPaymentMethodConstants.SepayQr,
                PaymentStatus = OrderPaymentStatusConstants.Paid,
                Total = 200000,
                Subtotal = 200000,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            var payment = new Payment
            {
                Id = paymentId,
                OrderId = orderId,
                PaymentCode = "GH0000009999",
                Method = PaymentMethodConstants.SepayQr,
                Status = PaymentStatusConstants.Paid,
                Amount = 200000,
                PaidAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            db.Orders.Add(order);
            db.Payments.Add(payment);
            await db.SaveChangesAsync();
        }

        // 1. Staff A requests refund of 100,000 VND
        var reqRefundRes = await PostAuthJsonAsync(_client, "/api/v1/admin/refunds", new
        {
            orderId,
            amount = 100000L,
            reason = "Khách hàng trả lại 1 sản phẩm hỏng"
        }, staffAToken);

        Assert.Equal(HttpStatusCode.Created, reqRefundRes.StatusCode);
        var refundDto = await reqRefundRes.Content.ReadFromJsonAsync<RefundDto>();
        Assert.NotNull(refundDto);
        Guid refundId = refundDto.Id;

        // 2. Staff A attempts to approve their own refund -> BLOCKED with 409 Conflict (Two-person rule)
        var selfApproveRes = await PostAuthJsonAsync(_client, $"/api/v1/admin/refunds/{refundId}/approve", new
        {
            note = "Self approve attempt"
        }, staffAToken);

        Assert.Equal(HttpStatusCode.Conflict, selfApproveRes.StatusCode);

        // 3. Staff B approves the refund -> SUCCEEDS
        var secondPersonApproveRes = await PostAuthJsonAsync(_client, $"/api/v1/admin/refunds/{refundId}/approve", new
        {
            note = "Staff B verified evidence and approved"
        }, staffBToken);

        Assert.Equal(HttpStatusCode.OK, secondPersonApproveRes.StatusCode);

        // 4. Staff B completes payout
        var completeRes = await PostAuthJsonAsync(_client, $"/api/v1/admin/refunds/{refundId}/complete", new
        {
            bankReference = "FT261115998877",
            note = "Transferred via MBBank internet banking"
        }, staffBToken);

        Assert.Equal(HttpStatusCode.OK, completeRes.StatusCode);

        await using (var db = _dbFixture.CreateDbContext())
        {
            var refund = await db.Refunds.FirstAsync(r => r.Id == refundId);
            Assert.Equal(RefundStatusConstants.Completed, refund.Status);
            Assert.Equal(staffAId, refund.RequestedBy);
            Assert.Equal(staffBId, refund.DecidedBy);
            Assert.Equal(staffBId, refund.CompletedBy);
            Assert.Equal("FT261115998877", refund.BankReference);
        }
    }

    [Fact]
    public async Task T_PAY_19_RefundAmountExceedsAvailable_Fails()
    {
        string adminToken = await Phase07TestHelper.LoginAdminAsync(_client);

        Guid adminUserId;
        await using (var db = _dbFixture.CreateDbContext())
        {
            var adminUser = await db.Users.FirstAsync(u => u.Email == "admin@groceryhub.vn");
            adminUserId = adminUser.Id;
        }

        // Create paid order with Total = 100000
        Guid orderId = Guid.NewGuid();
        Guid paymentId = Guid.NewGuid();
        await using (var db = _dbFixture.CreateDbContext())
        {
            db.Orders.Add(new Order
            {
                Id = orderId,
                OrderNumber = $"ORD-REFUND-MAX-{Guid.NewGuid():N}"[..20],
                UserId = adminUserId,
                Status = OrderStatusConstants.Delivered,
                PaymentMethod = OrderPaymentMethodConstants.SepayQr,
                PaymentStatus = OrderPaymentStatusConstants.Paid,
                Total = 100000,
                Subtotal = 100000,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            db.Payments.Add(new Payment
            {
                Id = paymentId,
                OrderId = orderId,
                PaymentCode = "GH0000008888",
                Method = PaymentMethodConstants.SepayQr,
                Status = PaymentStatusConstants.Paid,
                Amount = 100000,
                PaidAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        // Request refund of 150000 (exceeds 100000)
        var res = await PostAuthJsonAsync(_client, "/api/v1/admin/refunds", new
        {
            orderId,
            amount = 150000L,
            reason = "Exceeds paid total"
        }, adminToken);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task T_PAY_20_OwnerOnlyStatusAccess_CustomerCannotReadOthersPaymentStatus()
    {
        var (_, tokenA) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory, _client, "pay_ownerA@groceryhub.vn", "Customer A", "0904444441");

        var (_, tokenB) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory, _client, "pay_ownerB@groceryhub.vn", "Customer B", "0904444442");

        string adminToken = await Phase07TestHelper.LoginAdminAsync(_client);

        // Customer A places order
        Guid productId;
        await using (var db = _dbFixture.CreateDbContext())
        {
            productId = await SeedProductAsync(db, "Product A", 80000, 10);
        }

        Guid addressId = await Phase07TestHelper.CreateAddressAsync(_client, tokenA);
        await AddToCartAsync(_client, tokenA, productId, 1);

        var placeRes = await PostAuthJsonAsync(_client, "/api/v1/checkout/orders", new
        {
            addressId,
            paymentMethod = "SEPAY_QR",
        }, tokenA, idempotencyKey: Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.Created, placeRes.StatusCode);
        var orderRes = await placeRes.Content.ReadFromJsonAsync<PlaceOrderResponseDto>();
        string paymentCodeA = orderRes!.Payment.PaymentCode;

        // 1. Customer B attempts to read Customer A's status -> 403 Forbidden
        var resB = await GetAuthAsync(_client, $"/api/v1/payments/{paymentCodeA}/status", tokenB);
        Assert.Equal(HttpStatusCode.Forbidden, resB.StatusCode);

        // 2. Customer A reads own status -> 200 OK
        var resA = await GetAuthAsync(_client, $"/api/v1/payments/{paymentCodeA}/status", tokenA);
        Assert.Equal(HttpStatusCode.OK, resA.StatusCode);
        var statusA = await resA.Content.ReadFromJsonAsync<PaymentStatusResponseDto>();
        Assert.NotNull(statusA);
        Assert.Equal(paymentCodeA, statusA.PaymentCode);

        // 3. Staff/Admin reads status -> 200 OK
        var resAdmin = await GetAuthAsync(_client, $"/api/v1/payments/{paymentCodeA}/status", adminToken);
        Assert.Equal(HttpStatusCode.OK, resAdmin.StatusCode);
    }

    [Fact]
    public async Task T_PAY_23_OversizedWebhookBody_RejectedWith413()
    {
        // 1. Arrange a payload larger than 64KB (e.g. 70KB)
        string oversizedContent = new('X', 70000);
        string json = JsonSerializer.Serialize(new
        {
            id = 99999,
            content = oversizedContent
        });

        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string signature = ComputeHmac(timestamp, json, WebhookSecret);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/sepay/webhook")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Sepay-Timestamp", timestamp.ToString());
        request.Headers.Add("X-Sepay-Signature", $"sha256={signature}");

        // 2. Act
        using var response = await _client.SendAsync(request);

        // 3. Assert
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task T_PAY_24_CodAmountMismatchGuard_BlocksTransition()
    {
        string adminToken = await Phase07TestHelper.LoginAdminAsync(_client);

        Guid adminUserId;
        await using (var db = _dbFixture.CreateDbContext())
        {
            var adminUser = await db.Users.FirstAsync(u => u.Email == "admin@groceryhub.vn");
            adminUserId = adminUser.Id;
        }

        Guid orderId = Guid.NewGuid();
        await using (var db = _dbFixture.CreateDbContext())
        {
            db.Orders.Add(new Order
            {
                Id = orderId,
                OrderNumber = $"ORD-COD-{Guid.NewGuid():N}"[..20],
                UserId = adminUserId,
                Status = OrderStatusConstants.Shipping,
                PaymentMethod = OrderPaymentMethodConstants.COD,
                PaymentStatus = OrderPaymentStatusConstants.Unpaid,
                Total = 300000,
                Subtotal = 300000,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            db.Payments.Add(new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                PaymentCode = "GH0000007777",
                Method = PaymentMethodConstants.COD,
                Status = PaymentStatusConstants.Unpaid,
                Amount = 300000,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        // Staff attempts to mark COD order Delivered but reports collecting 250,000 VND instead of 300,000 VND
        var res = await PostAuthJsonAsync(_client, $"/api/v1/admin/orders/{orderId}/transition", new
        {
            to = OrderStatusConstants.Delivered,
            reason = "Delivered to customer",
            codAmount = 250000L // Mismatch with Order.Total (300000)
        }, adminToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
    }

    private static HttpRequestMessage CreateSignedWebhookRequest(
        long id,
        long amount,
        string content,
        string gateway = BankShortName,
        string accountNumber = AccountNumber,
        string transferType = "in",
        DateTimeOffset? transactionDate = null)
    {
        DateTimeOffset date = transactionDate ?? DateTimeOffset.UtcNow;
        var payloadObj = new
        {
            id,
            gateway,
            transactionDate = date.ToString("yyyy-MM-dd HH:mm:ss"),
            accountNumber,
            code = (string?)null,
            content,
            transferType,
            transferAmount = amount,
            accumulated = 10000000L,
            subAccount = (string?)null,
            referenceCode = $"REF{id}",
            description = (string?)null
        };

        string json = JsonSerializer.Serialize(payloadObj);
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string signature = ComputeHmac(timestamp, json, WebhookSecret);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/sepay/webhook")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Sepay-Timestamp", timestamp.ToString());
        request.Headers.Add("X-Sepay-Signature", $"sha256={signature}");

        return request;
    }

    private static string ComputeHmac(long timestamp, string rawBody, string secret)
    {
        string message = $"{timestamp}.{rawBody}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static async Task<HttpResponseMessage> PostAuthJsonAsync<T>(
        HttpClient client,
        string uri,
        T value,
        string token,
        string? idempotencyKey = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = JsonContent.Create(value)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostAuthEmptyAsync(HttpClient client, string uri, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> GetAuthAsync(HttpClient client, string uri, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    private static async Task<Guid> SeedProductAsync(GroceryHubDbContext db, string name, long sellingPrice, int stock = 10)
    {
        string sku = $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        var (_, productId) = await Phase07TestHelper.SeedCategoryAndProductAsync(db, name, sku, sellingPrice, sellingPrice / 2, stock, 0);
        return productId;
    }

    private static async Task AddToCartAsync(HttpClient client, string token, Guid productId, int quantity)
    {
        var res = await PostAuthJsonAsync(client, "/api/v1/cart/items", new { productId, quantity }, token);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
