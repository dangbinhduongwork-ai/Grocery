using System.Text;
using GroceryHub.Modules.Catalog.Application.Storage;
using GroceryHub.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;
using Xunit;

namespace GroceryHub.UnitTests.Catalog;

public sealed class ImageUploadValidatorTests
{
    private readonly InMemoryFileStorage _fileStorage = new();
    private readonly ImageUploadService _service;

    public ImageUploadValidatorTests()
    {
        _service = new ImageUploadService(_fileStorage, NullLogger<ImageUploadService>.Instance);
    }

    [Fact]
    public async Task ProcessImage_ValidPng_SucceedsAndReEncodes()
    {
        byte[] validPng = CreateBitmapBytes(SKEncodedImageFormat.Png);
        using MemoryStream stream = new(validPng);

        Result<SanitizedImageResult> result = await _service.ProcessAndStoreImageAsync(
            stream,
            "image/png",
            validPng.Length);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.EndsWith(".png", result.Value.StorageKey, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("image/png", result.Value.ContentType);
        Assert.Equal(10, result.Value.Width);
        Assert.Equal(10, result.Value.Height);
        Assert.Contains(result.Value.StorageKey, _fileStorage.StoredFiles.Keys);
    }

    [Fact]
    public async Task ProcessImage_ValidJpeg_SucceedsAndReEncodes()
    {
        byte[] validJpeg = CreateBitmapBytes(SKEncodedImageFormat.Jpeg);
        using MemoryStream stream = new(validJpeg);

        Result<SanitizedImageResult> result = await _service.ProcessAndStoreImageAsync(
            stream,
            "image/jpeg",
            validJpeg.Length);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.EndsWith(".jpg", result.Value.StorageKey, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("image/jpeg", result.Value.ContentType);
    }

    [Fact]
    public async Task ProcessImage_ValidWebp_SucceedsAndReEncodes()
    {
        byte[] validWebp = CreateBitmapBytes(SKEncodedImageFormat.Webp);
        using MemoryStream stream = new(validWebp);

        Result<SanitizedImageResult> result = await _service.ProcessAndStoreImageAsync(
            stream,
            "image/webp",
            validWebp.Length);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.EndsWith(".webp", result.Value.StorageKey, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("image/webp", result.Value.ContentType);
    }

    [Fact]
    public async Task ProcessImage_OversizedFile_FailsWithFileTooLarge()
    {
        // 5 MB + 1 byte
        long oversized = (5 * 1024 * 1024) + 1;
        using MemoryStream stream = new([0xFF, 0xD8, 0xFF, 0xE0]);

        Result<SanitizedImageResult> result = await _service.ProcessAndStoreImageAsync(
            stream,
            "image/jpeg",
            oversized);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.FileTooLarge, result.Error.Code);
    }

    [Fact]
    public async Task ProcessImage_DisallowedMime_FailsWithFileTypeNotAllowed()
    {
        byte[] bytes = [0x25, 0x50, 0x44, 0x46]; // PDF
        using MemoryStream stream = new(bytes);

        Result<SanitizedImageResult> result = await _service.ProcessAndStoreImageAsync(
            stream,
            "application/pdf",
            bytes.Length);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.FileTypeNotAllowed, result.Error.Code);
    }

    [Fact]
    public async Task ProcessImage_MismatchedMagicBytes_FailsWithFileTypeNotAllowed()
    {
        byte[] fakeJpeg = Encoding.UTF8.GetBytes("This is not a real JPEG image file");
        using MemoryStream stream = new(fakeJpeg);

        Result<SanitizedImageResult> result = await _service.ProcessAndStoreImageAsync(
            stream,
            "image/jpeg",
            fakeJpeg.Length);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.FileTypeNotAllowed, result.Error.Code);
    }

    [Fact]
    public async Task ProcessImage_SvgFile_FailsWithFileTypeNotAllowed()
    {
        byte[] svgBytes = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert('xss')</script></svg>");
        using MemoryStream stream = new(svgBytes);

        Result<SanitizedImageResult> result = await _service.ProcessAndStoreImageAsync(
            stream,
            "image/png",
            svgBytes.Length);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.FileTypeNotAllowed, result.Error.Code);
    }

    [Fact]
    public async Task ProcessImage_ExecutableBinary_FailsWithFileTypeNotAllowed()
    {
        // MZ executable header
        byte[] mzExe = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00];
        using MemoryStream stream = new(mzExe);

        Result<SanitizedImageResult> result = await _service.ProcessAndStoreImageAsync(
            stream,
            "image/jpeg",
            mzExe.Length);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.FileTypeNotAllowed, result.Error.Code);
    }

    private static byte[] CreateBitmapBytes(SKEncodedImageFormat format)
    {
        using SKBitmap bitmap = new(10, 10);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.Blue);

        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(format, 90);
        return data.ToArray();
    }

    private sealed class InMemoryFileStorage : IFileStorage
    {
        public Dictionary<string, byte[]> StoredFiles { get; } = new(StringComparer.Ordinal);

        public Task<string> SaveAsync(Stream stream, string filename, string contentType, CancellationToken ct = default)
        {
            using MemoryStream ms = new();
            stream.CopyTo(ms);
            StoredFiles[filename] = ms.ToArray();
            return Task.FromResult(filename);
        }

        public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct = default)
        {
            if (StoredFiles.TryGetValue(storageKey, out byte[]? bytes))
            {
                return Task.FromResult<Stream?>(new MemoryStream(bytes));
            }

            return Task.FromResult<Stream?>(null);
        }

        public Task<bool> DeleteAsync(string storageKey, CancellationToken ct = default)
        {
            return Task.FromResult(StoredFiles.Remove(storageKey));
        }

        public string GetPublicUrl(string storageKey) => $"/api/v1/catalog/images/{storageKey}";
    }
}
