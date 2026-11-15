using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GroceryHub.Modules.Customers.Application.DTOs;
using GroceryHub.Modules.Ordering.Application.DTOs;
using GroceryHub.Persistence;
using GroceryHub.SharedKernel;
using Xunit;

namespace GroceryHub.IntegrationTests.Ordering;

[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class OrderOwnershipIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
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
    public async Task Idor_CustomerA_CannotViewOrMutate_CustomerB_Orders_Addresses_OrCart()
    {
        // 1. Arrange Product
        Guid productId;
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            var (_, pId) = await Phase07TestHelper.SeedCategoryAndProductAsync(
                db,
                "Trứng gà Ba Huân Hộp 10 quả",
                $"SKU-EGG-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                sellingPrice: 35_000L,
                costPrice: 28_000L,
                onHand: 50,
                reserved: 0);
            productId = pId;
        }

        // 2. Register Customer A and Customer B
        string emailA = $"custA.idor.{Guid.NewGuid():N}@groceryhub.vn";
        var (userIdA, tokenA) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory,
            _client,
            emailA,
            "Customer A IDOR",
            "0919991111");

        string emailB = $"custB.idor.{Guid.NewGuid():N}@groceryhub.vn";
        var (userIdB, tokenB) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory,
            _client,
            emailB,
            "Customer B IDOR",
            "0919992222");

        // 3. Customer B creates Address B and places Order B
        Guid addressIdB = await Phase07TestHelper.CreateAddressAsync(
            _client,
            tokenB,
            recipientName: "Customer B Recipient",
            phone: "0919992222",
            provinceCode: "79",
            wardCode: "79001",
            street: "456 Dong Khoi, Ben Nghe, Quan 1");

        // Customer B adds item and places order
        AddCartItemRequest addReqB = new(productId, 2);
        using HttpRequestMessage addCartBMsg = new(HttpMethod.Post, "/api/v1/cart/items")
        {
            Content = JsonContent.Create(addReqB)
        };
        addCartBMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        using HttpResponseMessage addCartBRes = await _client.SendAsync(addCartBMsg);
        Assert.Equal(HttpStatusCode.OK, addCartBRes.StatusCode);

        string idempotencyKeyB = Guid.NewGuid().ToString("N");
        PlaceOrderRequest orderReqB = new(addressIdB, null, "COD", "Customer B order note");
        using HttpRequestMessage placeOrderBMsg = new(HttpMethod.Post, "/api/v1/checkout/orders")
        {
            Content = JsonContent.Create(orderReqB)
        };
        placeOrderBMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        placeOrderBMsg.Headers.Add("Idempotency-Key", idempotencyKeyB);
        using HttpResponseMessage placeOrderBRes = await _client.SendAsync(placeOrderBMsg);
        Assert.Equal(HttpStatusCode.Created, placeOrderBRes.StatusCode);

        PlaceOrderResponseDto? orderB = await placeOrderBRes.Content.ReadFromJsonAsync<PlaceOrderResponseDto>();
        Assert.NotNull(orderB);
        Guid orderIdB = orderB!.Order.Id;

        // 4. Test IDOR 1: Customer A attempts GET /me/orders/{orderIdB}
        using HttpRequestMessage getOrderMsg = new(HttpMethod.Get, $"/api/v1/me/orders/{orderIdB}");
        getOrderMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        using HttpResponseMessage getOrderRes = await _client.SendAsync(getOrderMsg);
        Assert.True(
            getOrderRes.StatusCode == HttpStatusCode.Forbidden || getOrderRes.StatusCode == HttpStatusCode.NotFound,
            $"Expected 403 or 404, but received {getOrderRes.StatusCode}");

        // 5. Test IDOR 2: Customer A attempts POST /me/orders/{orderIdB}/cancel
        CancelOrderRequest cancelReq = new("Malicious cancel attempt by Customer A");
        using HttpRequestMessage cancelMsg = new(HttpMethod.Post, $"/api/v1/me/orders/{orderIdB}/cancel")
        {
            Content = JsonContent.Create(cancelReq)
        };
        cancelMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        using HttpResponseMessage cancelRes = await _client.SendAsync(cancelMsg);
        Assert.True(
            cancelRes.StatusCode == HttpStatusCode.Forbidden || cancelRes.StatusCode == HttpStatusCode.NotFound,
            $"Expected 403 or 404, but received {cancelRes.StatusCode}");

        // 6. Test IDOR 3: Customer A attempts PUT /me/addresses/{addressIdB}
        UpdateAddressRequest updateAddrReq = new(
            RecipientName: "Tampered Name",
            Phone: "0919991111",
            ProvinceCode: "79",
            WardCode: "79001",
            Street: "Tampered Street",
            Note: "Tampered Note",
            IsDefault: true);

        using HttpRequestMessage updateAddrMsg = new(HttpMethod.Put, $"/api/v1/me/addresses/{addressIdB}")
        {
            Content = JsonContent.Create(updateAddrReq)
        };
        updateAddrMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        using HttpResponseMessage updateAddrRes = await _client.SendAsync(updateAddrMsg);
        Assert.True(
            updateAddrRes.StatusCode == HttpStatusCode.Forbidden || updateAddrRes.StatusCode == HttpStatusCode.NotFound,
            $"Expected 403 or 404, but received {updateAddrRes.StatusCode}");

        // 7. Test IDOR 4: Customer A attempts DELETE /me/addresses/{addressIdB}
        using HttpRequestMessage deleteAddrMsg = new(HttpMethod.Delete, $"/api/v1/me/addresses/{addressIdB}");
        deleteAddrMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        using HttpResponseMessage deleteAddrRes = await _client.SendAsync(deleteAddrMsg);
        Assert.True(
            deleteAddrRes.StatusCode == HttpStatusCode.Forbidden || deleteAddrRes.StatusCode == HttpStatusCode.NotFound,
            $"Expected 403 or 404, but received {deleteAddrRes.StatusCode}");

        // 8. Test IDOR 5: Customer A attempts POST /checkout/orders using addressIdB
        // Customer A adds item to cart
        AddCartItemRequest addReqA = new(productId, 1);
        using HttpRequestMessage addCartAMsg = new(HttpMethod.Post, "/api/v1/cart/items")
        {
            Content = JsonContent.Create(addReqA)
        };
        addCartAMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        using HttpResponseMessage addCartARes = await _client.SendAsync(addCartAMsg);
        Assert.Equal(HttpStatusCode.OK, addCartARes.StatusCode);

        string idempotencyKeyA = Guid.NewGuid().ToString("N");
        PlaceOrderRequest hijackOrderReq = new(addressIdB, null, "COD", "Hijacking Address B");
        using HttpRequestMessage hijackOrderMsg = new(HttpMethod.Post, "/api/v1/checkout/orders")
        {
            Content = JsonContent.Create(hijackOrderReq)
        };
        hijackOrderMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        hijackOrderMsg.Headers.Add("Idempotency-Key", idempotencyKeyA);
        using HttpResponseMessage hijackOrderRes = await _client.SendAsync(hijackOrderMsg);
        Assert.True(
            hijackOrderRes.StatusCode == HttpStatusCode.Forbidden || hijackOrderRes.StatusCode == HttpStatusCode.NotFound,
            $"Expected 403 or 404, but received {hijackOrderRes.StatusCode}");
    }
}
