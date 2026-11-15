using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GroceryHub.Modules.Ordering.Application.DTOs;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GroceryHub.IntegrationTests.Ordering;

[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class OrderLifecycleScriptedIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
{
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
    public async Task ScriptedLifecycle_FullPurchasingFlow_RecordsAllStepsAndMatchesDomainInvariants()
    {
        StringBuilder trace = new();
        trace.AppendLine("================================================================================");
        trace.AppendLine("GROCERYHUB PHASE 07 — SCRIPTED E2E PURCHASING FLOW EXECUTION TRACE");
        trace.AppendLine($"Executed At: {DateTimeOffset.UtcNow:O}");
        trace.AppendLine("Flow: Customer Register/Login -> Add Cart -> Quote -> Checkout COD -> Staff Confirm");
        trace.AppendLine("      -> Preparing -> Packed -> Shipping (Commit Stock) -> Delivered (COD Cash)");
        trace.AppendLine("================================================================================\n");

        // 1. Arrange Product in Database
        const long sellingPrice = 160_000L;
        const long costPrice = 130_000L;
        const int initialStock = 10;
        const int orderQuantity = 2;

        Guid productId;
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            var (_, pId) = await Phase07TestHelper.SeedCategoryAndProductAsync(
                db,
                "Gạo ST25 Ông Cua 5kg",
                $"SKU-ST25-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                sellingPrice,
                costPrice,
                initialStock,
                reserved: 0);
            productId = pId;
        }

        trace.AppendLine($"[Step 0] Seeded Product ID: {productId}, Price: {sellingPrice:N0}đ, Cost: {costPrice:N0}đ, Initial OnHand: {initialStock}\n");

        // 2. Register & Login Customer
        string customerEmail = $"customer.scripted.{Guid.NewGuid():N}@groceryhub.vn";
        var (customerId, customerToken) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory,
            _client,
            customerEmail,
            "Nguyen Van Scripted",
            "0912345678");

        trace.AppendLine($"[Step 1] Customer Registered & Logged In: {customerEmail} (ID: {customerId})");
        trace.AppendLine($"         Token acquired: Bearer {customerToken[..15]}...\n");

        // 3. Add Item to Cart
        AddCartItemRequest addReq = new(productId, orderQuantity);
        using HttpRequestMessage addCartHttpReq = new(HttpMethod.Post, "/api/v1/cart/items")
        {
            Content = JsonContent.Create(addReq)
        };
        addCartHttpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        using HttpResponseMessage addCartRes = await _client.SendAsync(addCartHttpReq);
        string addCartResBody = await addCartRes.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, addCartRes.StatusCode);
        trace.AppendLine("[Step 2] POST /api/v1/cart/items");
        trace.AppendLine($"         Request: {JsonSerializer.Serialize(addReq)}");
        trace.AppendLine($"         Response: {addCartRes.StatusCode} - {addCartResBody}\n");

        // 4. GET Cart
        using HttpRequestMessage getCartHttpReq = new(HttpMethod.Get, "/api/v1/cart");
        getCartHttpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        using HttpResponseMessage getCartRes = await _client.SendAsync(getCartHttpReq);
        string getCartResBody = await getCartRes.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, getCartRes.StatusCode);
        CartViewDto? cartView = JsonSerializer.Deserialize<CartViewDto>(getCartResBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(cartView);
        Assert.Single(cartView!.Items);
        Assert.Equal(sellingPrice * orderQuantity, cartView.Subtotal);

        trace.AppendLine("[Step 3] GET /api/v1/cart");
        trace.AppendLine($"         Response: {getCartRes.StatusCode} - Subtotal: {cartView.Subtotal:N0}đ, Items Count: {cartView.Items.Count}\n");

        // 5. Create Address for Customer
        Guid addressId = await Phase07TestHelper.CreateAddressAsync(
            _client,
            customerToken,
            recipientName: "Nguyen Van Scripted",
            phone: "0912345678",
            provinceCode: "79",
            wardCode: "79001",
            street: "77 Nguyen Hue, Ben Nghe, Quan 1");

        trace.AppendLine($"[Step 4] POST /api/v1/me/addresses -> Created Address ID: {addressId} (Province: 79 - TP.HCM)\n");

        // 6. POST /checkout/quote
        CheckoutQuoteRequest quoteReq = new(addressId, null, "COD");
        using HttpRequestMessage quoteHttpReq = new(HttpMethod.Post, "/api/v1/checkout/quote")
        {
            Content = JsonContent.Create(quoteReq)
        };
        quoteHttpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        using HttpResponseMessage quoteRes = await _client.SendAsync(quoteHttpReq);
        string quoteResBody = await quoteRes.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, quoteRes.StatusCode);
        CheckoutQuoteDto? quoteDto = JsonSerializer.Deserialize<CheckoutQuoteDto>(quoteResBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(quoteDto);
        Assert.Equal(sellingPrice * orderQuantity, quoteDto!.Subtotal);
        Assert.Contains("COD", quoteDto.AvailablePaymentMethods);

        trace.AppendLine("[Step 5] POST /api/v1/checkout/quote");
        trace.AppendLine($"         Request: {JsonSerializer.Serialize(quoteReq)}");
        trace.AppendLine($"         Response: {quoteRes.StatusCode} - Subtotal: {quoteDto.Subtotal:N0}đ, Shipping: {quoteDto.ShippingFee:N0}đ, Total: {quoteDto.Total:N0}đ, Available: [{string.Join(", ", quoteDto.AvailablePaymentMethods)}]\n");

        // 7. POST /checkout/orders (COD + Idempotency-Key)
        string idempotencyKey = Guid.NewGuid().ToString("N");
        PlaceOrderRequest orderReq = new(addressId, null, "COD", "Giao gio hanh chinh toa nha Sunwah");

        using HttpRequestMessage placeOrderHttpReq = new(HttpMethod.Post, "/api/v1/checkout/orders")
        {
            Content = JsonContent.Create(orderReq)
        };
        placeOrderHttpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        placeOrderHttpReq.Headers.Add("Idempotency-Key", idempotencyKey);
        using HttpResponseMessage placeOrderRes = await _client.SendAsync(placeOrderHttpReq);
        string placeOrderResBody = await placeOrderRes.Content.ReadAsStringAsync();

        Assert.True(placeOrderRes.StatusCode == HttpStatusCode.Created, $"Status was {placeOrderRes.StatusCode}, body: {placeOrderResBody}");
        PlaceOrderResponseDto? placedOrder = JsonSerializer.Deserialize<PlaceOrderResponseDto>(placeOrderResBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(placedOrder);
        Guid orderId = placedOrder!.Order.Id;
        string orderNumber = placedOrder.Order.OrderNumber;
        Assert.Equal("Placed", placedOrder.Order.Status);
        Assert.Equal("Unpaid", placedOrder.Payment.Status);

        trace.AppendLine("[Step 6] POST /api/v1/checkout/orders (with Idempotency-Key)");
        trace.AppendLine($"         Idempotency-Key: {idempotencyKey}");
        trace.AppendLine($"         Request: {JsonSerializer.Serialize(orderReq)}");
        trace.AppendLine($"         Response: {placeOrderRes.StatusCode} - OrderNumber: {orderNumber}, Status: {placedOrder.Order.Status}, PaymentStatus: {placedOrder.Payment.Status}\n");

        // Verify stock reserved in DB (OnHand = 10, Reserved = 2)
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            StockItem stock = await db.StockItems.FirstAsync(s => s.ProductId == productId);
            Assert.Equal(initialStock, stock.OnHand);
            Assert.Equal(orderQuantity, stock.Reserved);

            StockReservation reservation = await db.StockReservations.FirstAsync(r => r.OrderId == orderId);
            Assert.Equal("Active", reservation.Status);
            Assert.Equal(orderQuantity, reservation.Quantity);
        }

        trace.AppendLine($"         [DB State Verification] StockItems: OnHand = {initialStock}, Reserved = {orderQuantity}; StockReservations: Status = Active\n");

        // 8. Login Admin to create & login Staff
        string adminToken = await Phase07TestHelper.LoginAdminAsync(_client);
        string staffEmail = $"staff.scripted.{Guid.NewGuid():N}@groceryhub.vn";
        var (staffId, staffToken) = await Phase07TestHelper.CreateAndLoginStaffAsync(
            _client,
            adminToken,
            staffEmail,
            "Tran Van Staff",
            $"EMP-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}",
            role: "Staff",
            extraPermissions: ["orders.process", "orders.ship"]);

        trace.AppendLine($"[Step 7] Operations Staff Logged In: {staffEmail} (ID: {staffId}) with orders.process + orders.ship\n");

        // 9. Staff Transition: Placed -> Confirmed
        TransitionOrderRequest confirmReq = new("Confirmed", "Don hang hop le xac nhan xu ly", null);
        using HttpRequestMessage confirmHttpReq = new(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(confirmReq)
        };
        confirmHttpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        using HttpResponseMessage confirmRes = await _client.SendAsync(confirmHttpReq);
        Assert.Equal(HttpStatusCode.OK, confirmRes.StatusCode);

        trace.AppendLine($"[Step 8] POST /api/v1/admin/orders/{orderId}/transition -> Confirmed");
        trace.AppendLine($"         Response: {confirmRes.StatusCode}\n");

        // 10. Staff Transition: Confirmed -> Preparing
        TransitionOrderRequest prepReq = new("Preparing", "Nhan vien bat dau nhat hang tu ke", null);
        using HttpRequestMessage prepHttpReq = new(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(prepReq)
        };
        prepHttpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        using HttpResponseMessage prepRes = await _client.SendAsync(prepHttpReq);
        Assert.Equal(HttpStatusCode.OK, prepRes.StatusCode);

        trace.AppendLine($"[Step 9] POST /api/v1/admin/orders/{orderId}/transition -> Preparing");
        trace.AppendLine($"         Response: {prepRes.StatusCode}\n");

        // 11. Staff Transition: Preparing -> Packed
        TransitionOrderRequest packReq = new("Packed", "Kiem tra don hang va dong goi hoan tat", null);
        using HttpRequestMessage packHttpReq = new(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(packReq)
        };
        packHttpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        using HttpResponseMessage packRes = await _client.SendAsync(packHttpReq);
        Assert.Equal(HttpStatusCode.OK, packRes.StatusCode);

        trace.AppendLine($"[Step 10] POST /api/v1/admin/orders/{orderId}/transition -> Packed");
        trace.AppendLine($"          Response: {packRes.StatusCode}\n");

        // 12. Staff Transition: Packed -> Shipping (Commits Stock Reservation)
        TransitionOrderRequest shipReq = new("Shipping", "Xuat kho ban giao nhan vien giao hang", null);
        using HttpRequestMessage shipHttpReq = new(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(shipReq)
        };
        shipHttpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        using HttpResponseMessage shipRes = await _client.SendAsync(shipHttpReq);
        Assert.Equal(HttpStatusCode.OK, shipRes.StatusCode);

        trace.AppendLine($"[Step 11] POST /api/v1/admin/orders/{orderId}/transition -> Shipping");
        trace.AppendLine($"          Response: {shipRes.StatusCode}\n");

        // Verify stock committed in DB:
        // OnHand = initialStock - orderQuantity = 8, Reserved = 0, StockReservation = Committed
        // StockMovement = Type 'Sale', UnitCost = costPrice
        // OrderItem.UnitCostSnapshot = costPrice
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            StockItem stock = await db.StockItems.FirstAsync(s => s.ProductId == productId);
            Assert.Equal(initialStock - orderQuantity, stock.OnHand);
            Assert.Equal(0, stock.Reserved);

            StockReservation reservation = await db.StockReservations.FirstAsync(r => r.OrderId == orderId);
            Assert.Equal("Committed", reservation.Status);

            StockMovement movement = await db.StockMovements
                .FirstAsync(m => m.ProductId == productId && m.ReferenceId == orderId);
            Assert.Equal("Sale", movement.Type);
            Assert.Equal(-orderQuantity, movement.Quantity);
            Assert.Equal(costPrice, movement.UnitCost);

            OrderItem orderItem = await db.OrderItems.FirstAsync(i => i.OrderId == orderId && i.ProductId == productId);
            Assert.Equal(costPrice, orderItem.UnitCostSnapshot);
        }

        trace.AppendLine($"         [DB State Verification on Shipping]");
        trace.AppendLine($"         StockItems: OnHand = {initialStock - orderQuantity}, Reserved = 0 (Stock committed)");
        trace.AppendLine($"         StockReservations: Status = Committed");
        trace.AppendLine($"         StockMovements: Type = Sale, Qty = {-orderQuantity}, UnitCost = {costPrice:N0}đ");
        trace.AppendLine($"         OrderItem: UnitCostSnapshot = {costPrice:N0}đ\n");

        // 13. Staff Transition: Shipping -> Delivered (Records COD Cash Collection)
        long finalTotal = quoteDto.Total;
        TransitionOrderRequest deliverReq = new("Delivered", "Giao hang thanh cong va da thu tien mat", finalTotal);
        using HttpRequestMessage deliverHttpReq = new(HttpMethod.Post, $"/api/v1/admin/orders/{orderId}/transition")
        {
            Content = JsonContent.Create(deliverReq)
        };
        deliverHttpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        using HttpResponseMessage deliverRes = await _client.SendAsync(deliverHttpReq);
        Assert.Equal(HttpStatusCode.OK, deliverRes.StatusCode);

        trace.AppendLine($"[Step 12] POST /api/v1/admin/orders/{orderId}/transition -> Delivered (COD Collection)");
        trace.AppendLine($"          Collected COD Amount: {finalTotal:N0}đ");
        trace.AppendLine($"          Response: {deliverRes.StatusCode}\n");

        // Verify COD collection and final order status in DB
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            Order order = await db.Orders.FirstAsync(o => o.Id == orderId);
            Assert.Equal("Delivered", order.Status);
            Assert.Equal("Paid", order.PaymentStatus);

            Payment payment = await db.Payments.FirstAsync(p => p.OrderId == orderId);
            Assert.Equal("Paid", payment.Status);

            CodCollection codRecord = await db.CodCollections.FirstAsync(c => c.OrderId == orderId);
            Assert.Equal(finalTotal, codRecord.Amount);
            Assert.Equal(staffId, codRecord.CollectedBy);
        }

        trace.AppendLine($"         [DB State Verification on Delivered]");
        trace.AppendLine($"         Order Status: Delivered, PaymentStatus: Paid");
        trace.AppendLine($"         Payment Status: Paid");
        trace.AppendLine($"         payments.CodCollections: Amount = {finalTotal:N0}đ, CollectedBy = {staffId}");
        trace.AppendLine("================================================================================");
        trace.AppendLine("PURCHASING LIFECYCLE COMPLETED SUCCESSFULLY WITH ZERO INVARIANT VIOLATIONS.");
        trace.AppendLine("================================================================================");

        // Save execution trace to docs/evidence/phase-07/01-scripted-e2e-run.txt
        string evidenceDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "docs", "evidence", "phase-07");
        Directory.CreateDirectory(evidenceDir);
        string evidenceFilePath = Path.Combine(evidenceDir, "01-scripted-e2e-run.txt");
        await File.WriteAllTextAsync(evidenceFilePath, trace.ToString());
    }
}
