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
public sealed class CheckoutConcurrencyIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
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
    public async Task LastUnitStock_20ParallelCheckouts_Exactly1Succeeds_19FailWithInsufficientStock_NoOversell()
    {
        // 1. Arrange: Seed product with exactly 1 unit available (OnHand = 1, Reserved = 0)
        Guid productId;
        const long sellingPrice = 120_000L;
        const long costPrice = 90_000L;

        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            var (_, pId) = await Phase07TestHelper.SeedCategoryAndProductAsync(
                db,
                "Sữa đặc có đường Lon 380g (Last Unit)",
                $"SKU-LASTUNIT-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                sellingPrice,
                costPrice,
                onHand: 1,
                reserved: 0);
            productId = pId;
        }

        // 2. Prepare 20 distinct registered customers with address and item in cart
        const int concurrencyLevel = 20;
        List<(Guid UserId, string Token, Guid AddressId)> shoppers = [];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            string email = $"shopper.lastunit.{i}.{Guid.NewGuid():N}@groceryhub.vn";
            var (userId, token) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
                _factory,
                _client,
                email,
                $"Shopper LastUnit {i}",
                $"0912345{i:D3}");

            Guid addressId = await Phase07TestHelper.CreateAddressAsync(
                _client,
                token,
                recipientName: $"Shopper {i}",
                phone: $"0912345{i:D3}",
                provinceCode: "79",
                wardCode: "79001",
                street: $"Street {i}, Ben Nghe, Quan 1");

            // Add 1 unit to cart
            AddCartItemRequest addReq = new(productId, 1);
            using HttpRequestMessage addCartMsg = new(HttpMethod.Post, "/api/v1/cart/items")
            {
                Content = JsonContent.Create(addReq)
            };
            addCartMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using HttpResponseMessage addCartRes = await _client.SendAsync(addCartMsg);
            Assert.Equal(HttpStatusCode.OK, addCartRes.StatusCode);

            shoppers.Add((userId, token, addressId));
        }

        // 3. Act: Fire 20 parallel checkouts simultaneously
        Task<(HttpStatusCode StatusCode, string Body)>[] checkoutTasks = shoppers.Select(shopper => Task.Run(async () =>
        {
            using HttpClient localClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });

            string idempotencyKey = Guid.NewGuid().ToString("N");
            PlaceOrderRequest orderReq = new(shopper.AddressId, null, "COD", "Parallel checkout race");

            using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/checkout/orders")
            {
                Content = JsonContent.Create(orderReq)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", shopper.Token);
            request.Headers.Add("Idempotency-Key", idempotencyKey);

            using HttpResponseMessage response = await localClient.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();
            return (response.StatusCode, body);
        })).ToArray();

        (HttpStatusCode StatusCode, string Body)[] results = await Task.WhenAll(checkoutTasks);

        // 4. Assert: Exactly 1 201 Created, 19 409 Conflict
        int successes = results.Count(r => r.StatusCode == HttpStatusCode.Created);
        int conflicts = results.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(1, successes);
        Assert.Equal(19, conflicts);

        // Assert all 19 conflict responses indicate insufficient stock
        foreach (var failure in results.Where(r => r.StatusCode == HttpStatusCode.Conflict))
        {
            Assert.Contains("INSUFFICIENT_STOCK", failure.Body);
        }

        // Verify database: OnHand = 1, Reserved = 1, exactly 1 order in DB
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            StockItem stock = await db.StockItems.FirstAsync(s => s.ProductId == productId);
            Assert.Equal(1, stock.OnHand);
            Assert.Equal(1, stock.Reserved);

            int ordersCount = await db.Orders.CountAsync(o => o.Items.Any(i => i.ProductId == productId));
            Assert.Equal(1, ordersCount);

            int reservationsCount = await db.StockReservations.CountAsync(r => r.ProductId == productId && r.Status == "Active");
            Assert.Equal(1, reservationsCount);
        }
    }

    [Fact]
    public async Task CouponLimit_10ParallelCheckoutsForCouponWithLimit1_Exactly1Succeeds_9Fail()
    {
        // 1. Arrange: Seed a Coupon with UsageLimit = 1, PerUserLimit = 1
        string couponCode = $"CONC1-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        Guid couponId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            Coupon coupon = new()
            {
                Id = couponId,
                Code = couponCode,
                Name = "Giảm 30K Giới Hạn 1 Lần",
                Description = "Coupon chỉ dùng được đúng 1 lần toàn hệ thống",
                Type = "FixedAmount",
                Value = 30_000L,
                MaxDiscount = null,
                MinSubtotal = 100_000L,
                StartsAt = now.AddDays(-1),
                EndsAt = now.AddDays(30),
                UsageLimit = 1,
                PerUserLimit = 1,
                UsedCount = 0,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Coupons.Add(coupon);
            await db.SaveChangesAsync();
        }

        // Seed product with ample stock (OnHand = 100)
        Guid productId;
        const long sellingPrice = 150_000L;
        const long costPrice = 110_000L;

        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            var (_, pId) = await Phase07TestHelper.SeedCategoryAndProductAsync(
                db,
                "Dầu ăn Tường An 1L (Coupon Race)",
                $"SKU-OIL-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                sellingPrice,
                costPrice,
                onHand: 100,
                reserved: 0);
            productId = pId;
        }

        // Prepare 10 distinct registered customers, each with address and item in cart
        const int concurrencyLevel = 10;
        List<(Guid UserId, string Token, Guid AddressId)> shoppers = [];

        for (int i = 0; i < concurrencyLevel; i++)
        {
            string email = $"shopper.coupon.{i}.{Guid.NewGuid():N}@groceryhub.vn";
            var (userId, token) = await Phase07TestHelper.RegisterVerifyAndLoginCustomerAsync(
                _factory,
                _client,
                email,
                $"Shopper Coupon {i}",
                $"0913333{i:D3}");

            Guid addressId = await Phase07TestHelper.CreateAddressAsync(
                _client,
                token,
                recipientName: $"Shopper {i}",
                phone: $"0913333{i:D3}",
                provinceCode: "79",
                wardCode: "79001",
                street: $"Street {i}, Ben Nghe, Quan 1");

            // Add item to cart
            AddCartItemRequest addReq = new(productId, 1);
            using HttpRequestMessage addCartMsg = new(HttpMethod.Post, "/api/v1/cart/items")
            {
                Content = JsonContent.Create(addReq)
            };
            addCartMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using HttpResponseMessage addCartRes = await _client.SendAsync(addCartMsg);
            Assert.Equal(HttpStatusCode.OK, addCartRes.StatusCode);

            shoppers.Add((userId, token, addressId));
        }

        // 2. Act: Fire 10 parallel checkouts each applying the single-use coupon
        Task<(HttpStatusCode StatusCode, string Body)>[] checkoutTasks = shoppers.Select(shopper => Task.Run(async () =>
        {
            using HttpClient localClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });

            string idempotencyKey = Guid.NewGuid().ToString("N");
            PlaceOrderRequest orderReq = new(shopper.AddressId, couponCode, "COD", "Coupon race checkout");

            using HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/checkout/orders")
            {
                Content = JsonContent.Create(orderReq)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", shopper.Token);
            request.Headers.Add("Idempotency-Key", idempotencyKey);

            using HttpResponseMessage response = await localClient.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();
            return (response.StatusCode, body);
        })).ToArray();

        (HttpStatusCode StatusCode, string Body)[] results = await Task.WhenAll(checkoutTasks);

        // 3. Assert: Exactly 1 succeeds, 9 fail
        int successes = results.Count(r => r.StatusCode == HttpStatusCode.Created);
        int failures = results.Count(r => r.StatusCode != HttpStatusCode.Created);

        Assert.Equal(1, successes);
        Assert.Equal(9, failures);

        // Verify database: Coupon UsedCount == 1, exactly 1 redemption recorded
        await using (GroceryHubDbContext db = _dbFixture.CreateDbContext())
        {
            Coupon couponInDb = await db.Coupons.FirstAsync(c => c.Id == couponId);
            Assert.Equal(1, couponInDb.UsedCount);

            int redemptionsCount = await db.CouponRedemptions.CountAsync(r => r.CouponId == couponId && r.ReleasedAt == null);
            Assert.Equal(1, redemptionsCount);
        }
    }
}
