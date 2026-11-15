using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GroceryHub.Modules.Customers.Application.DTOs;
using GroceryHub.Modules.Identity.Contracts;
using GroceryHub.Modules.Ordering.Application.DTOs;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using GroceryHub.Persistence.Seeding;
using GroceryHub.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GroceryHub.IntegrationTests.Customers;

[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class CustomerAndCartIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
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
    public async Task GuestCartMergeOnLogin_MergedAndCappedAtMinAvailableAndMaxLineQuantity()
    {
        // 1. Arrange Product with OnHand = 30
        Guid productId;
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            var (_, pId) = await Phase07TestHelper.SeedCategoryAndProductAsync(
                db,
                "Bánh Chocopie Hộp 12 cái (Cart Merge)",
                $"SKU-MERGE-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                sellingPrice: 55_000L,
                costPrice: 42_000L,
                onHand: 30,
                reserved: 0);
            productId = pId;
        }

        // 2. Guest puts 15 units into anonymous cart
        AddCartItemRequest guestAddReq = new(productId, 15);
        using HttpResponseMessage guestAddRes = await _client.PostAsJsonAsync("/api/v1/cart/items", guestAddReq);
        Assert.Equal(HttpStatusCode.OK, guestAddRes.StatusCode);

        // Extract gh_cart cookie from response
        Assert.True(guestAddRes.Headers.TryGetValues("Set-Cookie", out var setCookies));
        string? ghCartCookie = setCookies!.FirstOrDefault(c => c.StartsWith("gh_cart=", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(ghCartCookie);
        string guestToken = ghCartCookie!.Split(';')[0].Split('=')[1];

        // 3. Register user and add 10 units to user cart
        string email = $"cart.merge.{Guid.NewGuid():N}@groceryhub.vn";
        const string password = "Password@123456";
        var (userId, userToken) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory,
            _client,
            email,
            "Nguyen Van Merge",
            "0918881234",
            password);

        AddCartItemRequest userAddReq = new(productId, 10);
        using HttpRequestMessage userAddMsg = new(HttpMethod.Post, "/api/v1/cart/items")
        {
            Content = JsonContent.Create(userAddReq)
        };
        userAddMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        using HttpResponseMessage userAddRes = await _client.SendAsync(userAddMsg);
        Assert.Equal(HttpStatusCode.OK, userAddRes.StatusCode);

        // 4. Act: User logs in again, sending the gh_cart cookie to trigger cart merge
        using HttpRequestMessage loginMsg = new(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new LoginRequest(email, password))
        };
        loginMsg.Headers.Add("Cookie", $"gh_cart={guestToken}");
        using HttpResponseMessage loginRes = await _client.SendAsync(loginMsg);
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);

        AuthTokenResponse? loginAuth = await loginRes.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(loginAuth);
        string newAccessToken = loginAuth!.AccessToken;

        // 5. Assert: Cart quantity is capped at min(available=30, MaxQuantityPerLine=20) = 20 (not 10 + 15 = 25)
        using HttpRequestMessage getCartMsg = new(HttpMethod.Get, "/api/v1/cart");
        getCartMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);
        using HttpResponseMessage getCartRes = await _client.SendAsync(getCartMsg);
        Assert.Equal(HttpStatusCode.OK, getCartRes.StatusCode);

        CartViewDto? mergedCart = await getCartRes.Content.ReadFromJsonAsync<CartViewDto>();
        Assert.NotNull(mergedCart);
        CartItemViewDto cartItem = Assert.Single(mergedCart!.Items);
        Assert.Equal(productId, cartItem.ProductId);
        Assert.Equal(20, cartItem.Quantity);

        // Verify guest cart was deleted in DB
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            bool guestCartExists = await db.Carts.AnyAsync(c => c.GuestCartToken == guestToken);
            Assert.False(guestCartExists);
        }
    }

    [Fact]
    public async Task AddressLimit_11thActiveAddressRejected_WithAddressLimitReached()
    {
        string email = $"addr.limit.{Guid.NewGuid():N}@groceryhub.vn";
        var (userId, token) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory,
            _client,
            email,
            "Nguyen Van AddressLimit",
            "0918882233");

        // 1. Create 10 active addresses (max allowed)
        List<Guid> createdAddressIds = [];
        for (int i = 1; i <= 10; i++)
        {
            CreateAddressRequest req = new(
                RecipientName: $"Recipient {i}",
                Phone: $"0918882{i:D3}",
                ProvinceCode: "79",
                WardCode: "79001",
                Street: $"Street number {i}, Ben Nghe, Quan 1",
                Note: $"Note {i}",
                IsDefault: i == 1);

            using HttpRequestMessage createMsg = new(HttpMethod.Post, "/api/v1/me/addresses")
            {
                Content = JsonContent.Create(req)
            };
            createMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using HttpResponseMessage createRes = await _client.SendAsync(createMsg);
            Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);

            AddressDto? created = await createRes.Content.ReadFromJsonAsync<AddressDto>();
            Assert.NotNull(created);
            createdAddressIds.Add(created!.Id);
        }

        // 2. Act: Attempt to create the 11th active address
        CreateAddressRequest req11 = new(
            RecipientName: "Recipient 11 (Excess)",
            Phone: "0918882999",
            ProvinceCode: "79",
            WardCode: "79001",
            Street: "Street 11",
            Note: "Excess address",
            IsDefault: false);

        using HttpRequestMessage excessMsg = new(HttpMethod.Post, "/api/v1/me/addresses")
        {
            Content = JsonContent.Create(req11)
        };
        excessMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage excessRes = await _client.SendAsync(excessMsg);
        string excessBody = await excessRes.Content.ReadAsStringAsync();

        // 3. Assert: 422 Unprocessable Entity with ADDRESS_LIMIT_REACHED
        Assert.Equal(HttpStatusCode.UnprocessableEntity, excessRes.StatusCode);
        Assert.Contains("ADDRESS_LIMIT_REACHED", excessBody);

        // 4. Soft-delete 1 non-default address, then 11th creation succeeds
        Guid toDelete = createdAddressIds[1];
        using HttpRequestMessage deleteMsg = new(HttpMethod.Delete, $"/api/v1/me/addresses/{toDelete}");
        deleteMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage deleteRes = await _client.SendAsync(deleteMsg);
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);

        // Now creating an address succeeds
        using HttpRequestMessage retryMsg = new(HttpMethod.Post, "/api/v1/me/addresses")
        {
            Content = JsonContent.Create(req11)
        };
        retryMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage retryRes = await _client.SendAsync(retryMsg);
        Assert.Equal(HttpStatusCode.Created, retryRes.StatusCode);
    }

    [Fact]
    public async Task NonServiceableProvince_RejectedWithAddressNotServiceable()
    {
        string email = $"shipping.nonserviceable.{Guid.NewGuid():N}@groceryhub.vn";
        var (_, token) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory,
            _client,
            email,
            "Customer NonServiceable",
            "0918883344");

        // Province "04" (Cao Bằng) is in administrative dataset but NOT in active shipping zones
        ShippingQuoteRequest quoteReq = new(AddressId: null, ProvinceCode: "04", Subtotal: 200_000L);
        using HttpRequestMessage msg = new(HttpMethod.Post, "/api/v1/shipping/quote")
        {
            Content = JsonContent.Create(quoteReq)
        };
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage res = await _client.SendAsync(msg);
        string body = await res.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
        Assert.Contains("ADDRESS_NOT_SERVICEABLE", body);
    }

    [Fact]
    public async Task CodLimitExceeded_TotalExceeding2MillionVnd_RejectedWithCodLimitExceeded()
    {
        // Arrange product with SellingPrice = 1.200.000đ
        Guid productId;
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            var (_, pId) = await Phase07TestHelper.SeedCategoryAndProductAsync(
                db,
                "Nồi chiên không dầu Philips 5L",
                $"SKU-AIRFRYER-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                sellingPrice: 1_200_000L,
                costPrice: 950_000L,
                onHand: 10,
                reserved: 0);
            productId = pId;
        }

        string email = $"cod.limit.{Guid.NewGuid():N}@groceryhub.vn";
        var (_, token) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory,
            _client,
            email,
            "Customer COD Limit",
            "0918884455");

        Guid addressId = await Phase07TestHelper.CreateAddressAsync(
            _client,
            token,
            recipientName: "Customer COD Limit",
            phone: "0918884455",
            provinceCode: "79",
            wardCode: "79001",
            street: "123 Le Loi, Ben Nghe, Quan 1");

        // Add 2 units -> Total = 2.400.000đ > 2.000.000đ COD limit
        AddCartItemRequest addReq = new(productId, 2);
        using HttpRequestMessage addCartMsg = new(HttpMethod.Post, "/api/v1/cart/items")
        {
            Content = JsonContent.Create(addReq)
        };
        addCartMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage addCartRes = await _client.SendAsync(addCartMsg);
        Assert.Equal(HttpStatusCode.OK, addCartRes.StatusCode);

        // Act: Attempt to place order with COD
        string idempotencyKey = Guid.NewGuid().ToString("N");
        PlaceOrderRequest orderReq = new(addressId, null, "COD", "Exceeding COD limit");

        using HttpRequestMessage orderMsg = new(HttpMethod.Post, "/api/v1/checkout/orders")
        {
            Content = JsonContent.Create(orderReq)
        };
        orderMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        orderMsg.Headers.Add("Idempotency-Key", idempotencyKey);
        using HttpResponseMessage orderRes = await _client.SendAsync(orderMsg);
        string orderBody = await orderRes.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, orderRes.StatusCode);
        Assert.Contains("COD_LIMIT_EXCEEDED", orderBody);
    }

    [Fact]
    public async Task AdministrativeUnits_Asserts34Provinces_3321Wards_AndDataVersionRecord()
    {
        // 1. GET /locations/provinces (AllowAnonymous)
        using HttpResponseMessage provincesRes = await _client.GetAsync("/api/v1/locations/provinces");
        Assert.Equal(HttpStatusCode.OK, provincesRes.StatusCode);

        ProvincesResponse? provincesDto = await provincesRes.Content.ReadFromJsonAsync<ProvincesResponse>();
        Assert.NotNull(provincesDto);
        Assert.Equal(AdminUnitSeeder.TargetProvinceCount, provincesDto!.Items.Count);
        Assert.Equal(34, provincesDto.Items.Count);

        // 2. GET /locations/provinces/79/wards
        using HttpResponseMessage wardsRes = await _client.GetAsync("/api/v1/locations/provinces/79/wards");
        Assert.Equal(HttpStatusCode.OK, wardsRes.StatusCode);

        WardsResponse? wardsDto = await wardsRes.Content.ReadFromJsonAsync<WardsResponse>();
        Assert.NotNull(wardsDto);
        Assert.Equal("79", wardsDto!.ProvinceCode);
        Assert.NotEmpty(wardsDto.Items);

        // 3. Direct DB assertions
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            int provinceCount = await db.Provinces.CountAsync();
            Assert.Equal(34, provinceCount);

            int wardCount = await db.Wards.CountAsync();
            Assert.Equal(AdminUnitSeeder.TargetWardCount, wardCount);
            Assert.Equal(3321, wardCount);

            AdminUnitDataVersion versionRecord = await db.AdminUnitDataVersions.FirstAsync();
            Assert.Equal(AdminUnitSeeder.DatasetVersion, versionRecord.Version);
            Assert.Equal(AdminUnitSeeder.DatasetSource, versionRecord.Source);
            Assert.Equal(AdminUnitSeeder.DatasetLicence, versionRecord.Licence);
            Assert.Equal(34, versionRecord.ProvinceCount);
            Assert.Equal(3321, versionRecord.WardCount);
        }
    }

    [Fact]
    public async Task PaymentMethodSepayQr_PlacesOrderWithPendingPaymentAndQrDetails()
    {
        Guid productId;
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            var (_, pId) = await Phase07TestHelper.SeedCategoryAndProductAsync(
                db,
                "Nước ngọt Coca-Cola 320ml",
                $"SKU-COKE-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                sellingPrice: 12_000L,
                costPrice: 9_000L,
                onHand: 10,
                reserved: 0);
            productId = pId;
        }

        string email = $"sepay.qr.{Guid.NewGuid():N}@groceryhub.vn";
        var (_, token) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
            _factory,
            _client,
            email,
            "Customer QR",
            "0918885566");

        Guid addressId = await Phase07TestHelper.CreateAddressAsync(
            _client,
            token,
            recipientName: "Customer QR",
            phone: "0918885566",
            provinceCode: "79",
            wardCode: "79001",
            street: "123 Le Loi, Ben Nghe, Quan 1");

        AddCartItemRequest addReq = new(productId, 2);
        using HttpRequestMessage addCartMsg = new(HttpMethod.Post, "/api/v1/cart/items")
        {
            Content = JsonContent.Create(addReq)
        };
        addCartMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage addCartRes = await _client.SendAsync(addCartMsg);
        Assert.Equal(HttpStatusCode.OK, addCartRes.StatusCode);

        // Checkout with SEPAY_QR (Enabled in Phase 10)
        string idempotencyKey = Guid.NewGuid().ToString("N");
        PlaceOrderRequest orderReq = new(addressId, null, "SEPAY_QR", "Checkout with VietQR in Phase 10");

        using HttpRequestMessage orderMsg = new(HttpMethod.Post, "/api/v1/checkout/orders")
        {
            Content = JsonContent.Create(orderReq)
        };
        orderMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        orderMsg.Headers.Add("Idempotency-Key", idempotencyKey);
        using HttpResponseMessage orderRes = await _client.SendAsync(orderMsg);

        Assert.Equal(HttpStatusCode.Created, orderRes.StatusCode);
        var orderBody = await orderRes.Content.ReadFromJsonAsync<PlaceOrderResponseDto>();
        Assert.NotNull(orderBody);
        Assert.Equal("PendingPayment", orderBody.Order.Status);
        Assert.Equal("Pending", orderBody.Payment.Status);
        Assert.Equal("SEPAY_QR", orderBody.Payment.Method);
        Assert.NotNull(orderBody.Payment.Qr);
        Assert.False(string.IsNullOrWhiteSpace(orderBody.Payment.Qr.Url));
        Assert.StartsWith("GH", orderBody.Payment.PaymentCode);

        // Also verify unsupported payment method returns error
        PlaceOrderRequest unsuppReq = new(addressId, null, "UNSUPPORTED_METHOD", "Testing unsupported method");
        using HttpRequestMessage unsuppMsg = new(HttpMethod.Post, "/api/v1/checkout/orders")
        {
            Content = JsonContent.Create(unsuppReq)
        };
        unsuppMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        unsuppMsg.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        using HttpResponseMessage unsuppRes = await _client.SendAsync(unsuppMsg);
        string unsuppBody = await unsuppRes.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unsuppRes.StatusCode);
        Assert.Contains("Payment method must be one of", unsuppBody);
    }
}
