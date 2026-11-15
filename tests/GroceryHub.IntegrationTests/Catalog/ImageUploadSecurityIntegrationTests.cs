using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GroceryHub.Modules.Catalog.Application.DTOs;
using GroceryHub.Modules.Identity.Contracts;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using Xunit;

namespace GroceryHub.IntegrationTests.Catalog;

[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class ImageUploadSecurityIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
{
    private readonly SqlServerTestDatabaseFixture _dbFixture = dbFixture;
    private GroceryHubWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private string _adminToken = null!;
    private Guid _productId;

    // Minimal valid 1x1 PNG (68 bytes)
    private static readonly byte[] Valid1x1PngBytes =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
        0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89, 0x00, 0x00, 0x00,
        0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
        0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49,
        0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
    ];

    public async Task InitializeAsync()
    {
        await _dbFixture.ResetDatabaseAsync();
        _factory = new GroceryHubWebApplicationFactory(_dbFixture.ConnectionString);
        _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        LoginRequest adminLogin = new("admin@groceryhub.vn", "Admin@GroceryHub2026!");
        using HttpResponseMessage adminLoginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", adminLogin);
        AuthTokenResponse? adminAuth = await adminLoginRes.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(adminAuth);
        _adminToken = adminAuth!.AccessToken;

        Guid categoryId = Guid.NewGuid();
        _productId = Guid.NewGuid();

        await using GroceryHubDbContext db = _dbFixture.CreateDbContext();
        Category category = new()
        {
            Id = categoryId,
            Name = "Bánh kẹo & Snack",
            Slug = $"banh-keo-snack-{Guid.NewGuid():N}",
            SortOrder = 1,
            IsActive = true
        };

        Product product = new()
        {
            Id = _productId,
            Sku = $"SKU-IMG-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            Name = "Bánh Chocopie Orion Hộp 12 Cái",
            Slug = $"banh-chocopie-orion-12-cai-{Guid.NewGuid():N}",
            SearchText = "banh chocopie orion hop 12 cai",
            CategoryId = categoryId,
            Unit = "Hộp",
            ListPrice = 60000L,
            SellingPrice = 55000L,
            CostPrice = 42000L,
            Status = ProductStatus.Active
        };

        db.Categories.Add(category);
        db.Products.Add(product);
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task UploadValidImage_Succeeds_ReturnsRandomStorageKey_AndServesThroughPublicEndpoint()
    {
        using MultipartFormDataContent form = new();
        using ByteArrayContent fileContent = new(Valid1x1PngBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "file", "original_client_filename.png");
        form.Add(new StringContent("Mat truoc hop banh Chocopie"), "alt");
        form.Add(new StringContent("true"), "isPrimary");

        using HttpRequestMessage request = new(HttpMethod.Post, $"/api/v1/admin/products/{_productId}/images")
        {
            Content = form
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        using HttpResponseMessage response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        ProductImageDto? imageDto = await response.Content.ReadFromJsonAsync<ProductImageDto>();
        Assert.NotNull(imageDto);
        Assert.NotEqual(Guid.Empty, imageDto!.Id);
        Assert.False(string.IsNullOrWhiteSpace(imageDto.Url));

        // Security check: Original filename must NEVER be preserved in the storage key or URL
        Assert.DoesNotContain("original_client_filename", imageDto.Url, StringComparison.OrdinalIgnoreCase);

        // Verify image can be fetched via public streaming endpoint
        using HttpResponseMessage streamRes = await _client.GetAsync(imageDto.Url);
        Assert.Equal(HttpStatusCode.OK, streamRes.StatusCode);
        byte[] fetchedBytes = await streamRes.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(fetchedBytes);
    }

    [Fact]
    public async Task UploadPhpScriptRenamedToJpg_FailsMagicBytesValidation()
    {
        // Non-image bytes disguised with .jpg extension and image/jpeg Content-Type
        byte[] scriptBytes = [0x3C, 0x3F, 0x70, 0x68, 0x70, 0x20, 0x65, 0x63, 0x68, 0x6F, 0x20, 0x27, 0x74, 0x65, 0x73, 0x74, 0x27, 0x3B];

        using MultipartFormDataContent form = new();
        using ByteArrayContent fileContent = new(scriptBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(fileContent, "file", "disguised_script.php.jpg");
        form.Add(new StringContent("Fake image attack"), "alt");

        using HttpRequestMessage request = new(HttpMethod.Post, $"/api/v1/admin/products/{_productId}/images")
        {
            Content = form
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        using HttpResponseMessage response = await _client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity,
            $"Expected 400 or 422, received: {response.StatusCode}");

        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("FILE_TYPE_NOT_ALLOWED", body);
    }

    [Fact]
    public async Task UploadOversizedFile_Exceeding5MB_FailsValidation()
    {
        // 5 MB = 5,242,880 bytes. Create 5 MB + 1024 bytes
        byte[] oversizedBytes = new byte[5 * 1024 * 1024 + 1024];
        // Prepend valid PNG signature so it fails purely on size check first
        Valid1x1PngBytes.CopyTo(oversizedBytes, 0);

        using MultipartFormDataContent form = new();
        using ByteArrayContent fileContent = new(oversizedBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "file", "huge_photo.png");

        using HttpRequestMessage request = new(HttpMethod.Post, $"/api/v1/admin/products/{_productId}/images")
        {
            Content = form
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        using HttpResponseMessage response = await _client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity,
            $"Expected 400 or 422, received: {response.StatusCode}");

        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("FILE_TOO_LARGE", body);
    }

    [Fact]
    public async Task UploadBadMimeType_PdfDocument_FailsValidation()
    {
        byte[] pdfBytes = "%PDF-1.4\n1 0 obj\n<<>>\nendobj\ntrailer\n<<>>\n%%EOF"u8.ToArray();

        using MultipartFormDataContent form = new();
        using ByteArrayContent fileContent = new(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", "invoice.pdf");

        using HttpRequestMessage request = new(HttpMethod.Post, $"/api/v1/admin/products/{_productId}/images")
        {
            Content = form
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);

        using HttpResponseMessage response = await _client.SendAsync(request);
        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity,
            $"Expected 400 or 422, received: {response.StatusCode}");

        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("FILE_TYPE_NOT_ALLOWED", body);
    }
}
