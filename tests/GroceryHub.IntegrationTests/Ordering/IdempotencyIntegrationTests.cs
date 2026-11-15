using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GroceryHub.Modules.Ordering.Application.DTOs;
using GroceryHub.Persistence;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GroceryHub.IntegrationTests.Ordering;

[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class IdempotencyIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
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
    public async Task ReplaySameIdempotencyKey_IdenticalBody_ReturnsSameOrder201_SingleOrderInDb()
    {
        // 1. Arrange: Seed product and customer
        Guid productId;
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            var (_, pId) = await Phase07TestHelper.SeedCategoryAndProductAsync(
                db,
                "Bột giặt OMO 3kg",
                $"SKU-OMO-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                sellingPrice: 145_000L,
                costPrice: 115_000L,
                onHand: 10,
                reserved: 0);
            productId = pId;
        }

        string email = $"idempotent.same.{Guid.NewGuid():N}@groceryhub.vn";
        var (userId, token) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory,
            _client,
            email,
            "Nguyen Van Idempotent",
            "0914445566");

        Guid addressId = await Phase07TestHelper.CreateAddressAsync(
            _client,
            token,
            recipientName: "Nguyen Van Idempotent",
            phone: "0914445566",
            provinceCode: "79",
            wardCode: "79001",
            street: "123 Le Loi, Ben Nghe, Quan 1");

        // Add 1 unit to cart
        AddCartItemRequest addReq = new(productId, 1);
        using HttpRequestMessage addCartMsg = new(HttpMethod.Post, "/api/v1/cart/items")
        {
            Content = JsonContent.Create(addReq)
        };
        addCartMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage addCartRes = await _client.SendAsync(addCartMsg);
        Assert.Equal(HttpStatusCode.OK, addCartRes.StatusCode);

        // 2. Act: Send first checkout request with Idempotency-Key
        string idempotencyKey = Guid.NewGuid().ToString("N");
        PlaceOrderRequest orderReq = new(addressId, null, "COD", "First delivery attempt note");

        using HttpRequestMessage firstReq = new(HttpMethod.Post, "/api/v1/checkout/orders")
        {
            Content = JsonContent.Create(orderReq)
        };
        firstReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        firstReq.Headers.Add("Idempotency-Key", idempotencyKey);

        using HttpResponseMessage firstRes = await _client.SendAsync(firstReq);
        string firstBody = await firstRes.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Created, firstRes.StatusCode);

        PlaceOrderResponseDto? firstOrder = JsonSerializer.Deserialize<PlaceOrderResponseDto>(firstBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(firstOrder);

        // 3. Replay exact same request with same Idempotency-Key
        using HttpRequestMessage replayReq = new(HttpMethod.Post, "/api/v1/checkout/orders")
        {
            Content = JsonContent.Create(orderReq)
        };
        replayReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        replayReq.Headers.Add("Idempotency-Key", idempotencyKey);

        using HttpResponseMessage replayRes = await _client.SendAsync(replayReq);
        string replayBody = await replayRes.Content.ReadAsStringAsync();

        // 4. Assert: Status is 201 Created and identical order payload is returned
        Assert.True(replayRes.StatusCode == HttpStatusCode.Created || replayRes.StatusCode == HttpStatusCode.OK);
        PlaceOrderResponseDto? replayedOrder = JsonSerializer.Deserialize<PlaceOrderResponseDto>(replayBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(replayedOrder);

        Assert.Equal(firstOrder!.Order.Id, replayedOrder!.Order.Id);
        Assert.Equal(firstOrder.Order.OrderNumber, replayedOrder.Order.OrderNumber);
        Assert.Equal(firstOrder.Order.Total, replayedOrder.Order.Total);

        // Verify DB contains strictly 1 order and 1 stock reservation
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            int ordersCount = await db.Orders.CountAsync(o => o.UserId == userId);
            Assert.Equal(1, ordersCount);

            int reservationsCount = await db.StockReservations.CountAsync(r => r.ProductId == productId);
            Assert.Equal(1, reservationsCount);

            int idempotencyCount = await db.IdempotencyRecords.CountAsync(r => r.Key == idempotencyKey);
            Assert.Equal(1, idempotencyCount);
        }
    }

    [Fact]
    public async Task ReplaySameIdempotencyKey_DifferentBody_Returns409Conflict_IdempotencyKeyReused()
    {
        // 1. Arrange: Seed product and customer
        Guid productId;
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            var (_, pId) = await Phase07TestHelper.SeedCategoryAndProductAsync(
                db,
                "Dầu gội Clear Men 650g",
                $"SKU-CLEAR-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                sellingPrice: 175_000L,
                costPrice: 135_000L,
                onHand: 10,
                reserved: 0);
            productId = pId;
        }

        string email = $"idempotent.diff.{Guid.NewGuid():N}@groceryhub.vn";
        var (userId, token) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory,
            _client,
            email,
            "Nguyen Van Idempotent Diff",
            "0915556677");

        Guid addressId = await Phase07TestHelper.CreateAddressAsync(
            _client,
            token,
            recipientName: "Nguyen Van Idempotent Diff",
            phone: "0915556677",
            provinceCode: "79",
            wardCode: "79001",
            street: "456 Le Duan, Ben Nghe, Quan 1");

        // Add 1 unit to cart
        AddCartItemRequest addReq = new(productId, 1);
        using HttpRequestMessage addCartMsg = new(HttpMethod.Post, "/api/v1/cart/items")
        {
            Content = JsonContent.Create(addReq)
        };
        addCartMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage addCartRes = await _client.SendAsync(addCartMsg);
        Assert.Equal(HttpStatusCode.OK, addCartRes.StatusCode);

        // 2. Act: Send first checkout request with Idempotency-Key
        string idempotencyKey = Guid.NewGuid().ToString("N");
        PlaceOrderRequest originalOrderReq = new(addressId, null, "COD", "Original delivery note");

        using HttpRequestMessage firstReq = new(HttpMethod.Post, "/api/v1/checkout/orders")
        {
            Content = JsonContent.Create(originalOrderReq)
        };
        firstReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        firstReq.Headers.Add("Idempotency-Key", idempotencyKey);

        using HttpResponseMessage firstRes = await _client.SendAsync(firstReq);
        Assert.Equal(HttpStatusCode.Created, firstRes.StatusCode);

        // 3. Replay with SAME Idempotency-Key but DIFFERENT body (different note)
        PlaceOrderRequest modifiedOrderReq = new(addressId, null, "COD", "TAMPERED or MODIFIED different delivery note!");

        using HttpRequestMessage secondReq = new(HttpMethod.Post, "/api/v1/checkout/orders")
        {
            Content = JsonContent.Create(modifiedOrderReq)
        };
        secondReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        secondReq.Headers.Add("Idempotency-Key", idempotencyKey);

        using HttpResponseMessage secondRes = await _client.SendAsync(secondReq);
        string secondBody = await secondRes.Content.ReadAsStringAsync();

        // 4. Assert: Status is HTTP 409 Conflict with IDEMPOTENCY_KEY_REUSED
        Assert.Equal(HttpStatusCode.Conflict, secondRes.StatusCode);
        Assert.Contains("IDEMPOTENCY_KEY_REUSED", secondBody);

        // Verify DB: Still only 1 order created
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            int ordersCount = await db.Orders.CountAsync(o => o.UserId == userId);
            Assert.Equal(1, ordersCount);
        }
    }
}
