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
public sealed class PriceTamperingIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
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
    public async Task ClientAttemptsPriceTampering_InjectedPricesAndTotalsAreIgnored_AuthoritativeCatalogPricesUsed()
    {
        // 1. Arrange: Seed product with authentic catalog price: SellingPrice = 250.000đ, CostPrice = 200.000đ
        const long genuineCatalogPrice = 250_000L;
        const long genuineCostPrice = 200_000L;
        const int quantity = 2;
        const long expectedGenuineSubtotal = genuineCatalogPrice * quantity; // 500.000đ

        Guid productId;
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            var (_, pId) = await Phase07TestHelper.SeedCategoryAndProductAsync(
                db,
                "Thịt bò Wagyu Úc 500g (Tamper Test)",
                $"SKU-TAMPER-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                genuineCatalogPrice,
                genuineCostPrice,
                onHand: 10,
                reserved: 0);
            productId = pId;
        }

        // Register and login customer
        string email = $"tamper.customer.{Guid.NewGuid():N}@groceryhub.vn";
        var (userId, token) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory,
            _client,
            email,
            "Nguyen Van Tamperer",
            "0916667788");

        Guid addressId = await Phase07TestHelper.CreateAddressAsync(
            _client,
            token,
            recipientName: "Nguyen Van Tamperer",
            phone: "0916667788",
            provinceCode: "79",
            wardCode: "79001",
            street: "123 Le Loi, Ben Nghe, Quan 1");

        // Add 2 units to cart
        AddCartItemRequest addReq = new(productId, quantity);
        using HttpRequestMessage addCartMsg = new(HttpMethod.Post, "/api/v1/cart/items")
        {
            Content = JsonContent.Create(addReq)
        };
        addCartMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage addCartRes = await _client.SendAsync(addCartMsg);
        Assert.Equal(HttpStatusCode.OK, addCartRes.StatusCode);

        // 2. Act: Client attempts to tamper with prices during /checkout/quote
        // Client sends a malicious payload containing fake low prices and zero total
        string tamperedQuotePayload = $$"""
        {
            "addressId": "{{addressId}}",
            "paymentMethod": "COD",
            "subtotal": 1000,
            "total": 1000,
            "shippingFee": 0,
            "unitPrice": 500,
            "discount": 499000
        }
        """;

        using HttpRequestMessage quoteMsg = new(HttpMethod.Post, "/api/v1/checkout/quote")
        {
            Content = new StringContent(tamperedQuotePayload, Encoding.UTF8, "application/json")
        };
        quoteMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage quoteRes = await _client.SendAsync(quoteMsg);
        string quoteBody = await quoteRes.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, quoteRes.StatusCode);
        CheckoutQuoteDto? quoteDto = JsonSerializer.Deserialize<CheckoutQuoteDto>(quoteBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(quoteDto);

        // Verify quote computed genuine values strictly from catalog
        Assert.Equal(expectedGenuineSubtotal, quoteDto!.Subtotal);
        Assert.True(quoteDto.Total >= expectedGenuineSubtotal);

        // 3. Act: Client attempts to tamper with prices in /checkout/orders
        string tamperedOrderPayload = $$"""
        {
            "addressId": "{{addressId}}",
            "paymentMethod": "COD",
            "note": "Attacker trying to pay 1000 VND only",
            "unitPrice": 500,
            "sellingPrice": 500,
            "subtotal": 1000,
            "total": 1000,
            "shippingFee": 0,
            "price": 500
        }
        """;

        string idempotencyKey = Guid.NewGuid().ToString("N");
        using HttpRequestMessage orderMsg = new(HttpMethod.Post, "/api/v1/checkout/orders")
        {
            Content = new StringContent(tamperedOrderPayload, Encoding.UTF8, "application/json")
        };
        orderMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        orderMsg.Headers.Add("Idempotency-Key", idempotencyKey);
        using HttpResponseMessage orderRes = await _client.SendAsync(orderMsg);
        string orderBody = await orderRes.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, orderRes.StatusCode);
        PlaceOrderResponseDto? createdOrder = JsonSerializer.Deserialize<PlaceOrderResponseDto>(orderBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(createdOrder);

        // 4. Assert: Created order response uses authentic values
        Assert.Equal(expectedGenuineSubtotal, createdOrder!.Order.Subtotal);
        Assert.Equal(quoteDto.Total, createdOrder.Order.Total);

        // 5. Assert: In database, Order and OrderItems strictly reflect live catalog prices
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            Order dbOrder = await db.Orders
                .Include(o => o.Items)
                .FirstAsync(o => o.Id == createdOrder.Order.Id);

            Assert.Equal(expectedGenuineSubtotal, dbOrder.Subtotal);
            Assert.Equal(quoteDto.Total, dbOrder.Total);

            OrderItem dbItem = Assert.Single(dbOrder.Items);
            Assert.Equal(genuineCatalogPrice, dbItem.UnitPrice);
            Assert.Equal(expectedGenuineSubtotal, dbItem.Subtotal);
            Assert.Equal(quantity, dbItem.Quantity);
        }
    }
}
