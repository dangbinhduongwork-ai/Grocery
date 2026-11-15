using System.Text;
using GroceryHub.SharedKernel;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace GroceryHub.Modules.Catalog.Application.Storage;

public sealed class ImageUploadService : IImageUploadService
{
    public const long MaxSizeBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private static readonly byte[] JpegMagic = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] RiffMagic = [(byte)'R', (byte)'I', (byte)'F', (byte)'F'];
    private static readonly byte[] WebpMagic = [(byte)'W', (byte)'E', (byte)'B', (byte)'P'];

    private readonly IFileStorage _fileStorage;
    private readonly ILogger<ImageUploadService> _logger;

    public ImageUploadService(
        IFileStorage fileStorage,
        ILogger<ImageUploadService> logger)
    {
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<SanitizedImageResult>> ProcessAndStoreImageAsync(
        Stream inputStream,
        string claimedMimeType,
        long reportedLength,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(inputStream);

        if (reportedLength > MaxSizeBytes)
        {
            return Result.Failure<SanitizedImageResult>(
                DomainError.Validation(ErrorCodes.FileTooLarge, $"Uploaded file exceeds maximum allowed size of {MaxSizeBytes / (1024 * 1024)} MB."));
        }

        string normalizedMime = (claimedMimeType ?? string.Empty).Trim().ToLowerInvariant();
        if (!AllowedMimeTypes.Contains(normalizedMime))
        {
            return Result.Failure<SanitizedImageResult>(
                DomainError.Validation(ErrorCodes.FileTypeNotAllowed, "Only JPEG, PNG, and WebP images are allowed."));
        }

        using MemoryStream ms = new();
        byte[] buffer = new byte[8192];
        long totalRead = 0;
        int read;

        while ((read = await inputStream.ReadAsync(buffer, ct)) > 0)
        {
            totalRead += read;
            if (totalRead > MaxSizeBytes)
            {
                return Result.Failure<SanitizedImageResult>(
                    DomainError.Validation(ErrorCodes.FileTooLarge, $"Uploaded file exceeds maximum allowed size of {MaxSizeBytes / (1024 * 1024)} MB."));
            }

            ms.Write(buffer, 0, read);
        }

        if (totalRead == 0)
        {
            return Result.Failure<SanitizedImageResult>(
                DomainError.Validation(ErrorCodes.FileTypeNotAllowed, "Uploaded file is empty."));
        }

        byte[] fileBytes = ms.ToArray();

        // 1. Check for text/XML/SVG/Script polyglots
        int headerCheckLen = Math.Min(fileBytes.Length, 512);
        string headerAscii = Encoding.ASCII.GetString(fileBytes, 0, headerCheckLen);
        if (headerAscii.Contains("<?xml", StringComparison.OrdinalIgnoreCase) ||
            headerAscii.Contains("<svg", StringComparison.OrdinalIgnoreCase) ||
            headerAscii.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
            headerAscii.Contains("<html", StringComparison.OrdinalIgnoreCase) ||
            headerAscii.Contains("<script", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<SanitizedImageResult>(
                DomainError.Validation(ErrorCodes.FileTypeNotAllowed, "SVG, HTML, XML, or script content is strictly prohibited."));
        }

        // Executable magic bytes check (MZ / ELF)
        if (fileBytes.Length >= 2 && fileBytes[0] == 0x4D && fileBytes[1] == 0x5A)
        {
            return Result.Failure<SanitizedImageResult>(
                DomainError.Validation(ErrorCodes.FileTypeNotAllowed, "Executable binaries are strictly prohibited."));
        }

        if (fileBytes.Length >= 4 && fileBytes[0] == 0x7F && fileBytes[1] == (byte)'E' && fileBytes[2] == (byte)'L' && fileBytes[3] == (byte)'F')
        {
            return Result.Failure<SanitizedImageResult>(
                DomainError.Validation(ErrorCodes.FileTypeNotAllowed, "Executable binaries are strictly prohibited."));
        }

        // 2. Validate magic bytes against claimed MIME type
        bool magicValid = ValidateMagicBytes(fileBytes, normalizedMime);
        if (!magicValid)
        {
            return Result.Failure<SanitizedImageResult>(
                DomainError.Validation(ErrorCodes.FileTypeNotAllowed, $"File header does not match claimed MIME type '{normalizedMime}'."));
        }

        // 3. Re-encode image via SkiaSharp to strip EXIF, metadata, and malicious trailing payloads
        try
        {
            using MemoryStream decodeMs = new(fileBytes);
            using SKBitmap? bitmap = SKBitmap.Decode(decodeMs);

            if (bitmap is null || bitmap.Width <= 0 || bitmap.Height <= 0)
            {
                return Result.Failure<SanitizedImageResult>(
                    DomainError.Validation(ErrorCodes.FileTypeNotAllowed, "Image could not be decoded or is corrupted."));
            }

            SKEncodedImageFormat encodeFormat = normalizedMime switch
            {
                "image/jpeg" => SKEncodedImageFormat.Jpeg,
                "image/png" => SKEncodedImageFormat.Png,
                "image/webp" => SKEncodedImageFormat.Webp,
                _ => SKEncodedImageFormat.Jpeg
            };

            using SKImage skImage = SKImage.FromBitmap(bitmap);
            using SKData? encoded = skImage.Encode(encodeFormat, 90);

            if (encoded is null)
            {
                return Result.Failure<SanitizedImageResult>(
                    DomainError.Validation(ErrorCodes.FileTypeNotAllowed, "Failed to re-encode and sanitize image."));
            }

            string extension = encodeFormat switch
            {
                SKEncodedImageFormat.Jpeg => ".jpg",
                SKEncodedImageFormat.Png => ".png",
                SKEncodedImageFormat.Webp => ".webp",
                _ => ".jpg"
            };

            string storageKey = $"{Guid.NewGuid():N}{extension}";

            using Stream sanitizedStream = encoded.AsStream();
            await _fileStorage.SaveAsync(sanitizedStream, storageKey, normalizedMime, ct);
            string publicUrl = _fileStorage.GetPublicUrl(storageKey);

            _logger.LogInformation("Sanitized and stored image {StorageKey} ({Width}x{Height}, {Format})",
                storageKey, bitmap.Width, bitmap.Height, encodeFormat);

            return Result.Success(new SanitizedImageResult(
                StorageKey: storageKey,
                Url: publicUrl,
                Width: bitmap.Width,
                Height: bitmap.Height,
                ContentType: normalizedMime));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process image upload");
            return Result.Failure<SanitizedImageResult>(
                DomainError.Validation(ErrorCodes.FileTypeNotAllowed, "Image processing failed due to invalid format or structure."));
        }
    }

    public Task<bool> DeleteStoredImageAsync(string storageKey, CancellationToken ct = default)
    {
        return _fileStorage.DeleteAsync(storageKey, ct);
    }

    public static bool ValidateMagicBytes(byte[] bytes, string mimeType)
    {
        if (bytes == null || bytes.Length < 4)
        {
            return false;
        }

        switch (mimeType.ToLowerInvariant())
        {
            case "image/jpeg":
                return bytes.Length >= 3 &&
                       bytes[0] == JpegMagic[0] &&
                       bytes[1] == JpegMagic[1] &&
                       bytes[2] == JpegMagic[2];

            case "image/png":
                return bytes.Length >= PngMagic.Length &&
                       bytes.Take(PngMagic.Length).SequenceEqual(PngMagic);

            case "image/webp":
                if (bytes.Length < 12)
                {
                    return false;
                }

                bool isRiff = bytes[0] == RiffMagic[0] &&
                              bytes[1] == RiffMagic[1] &&
                              bytes[2] == RiffMagic[2] &&
                              bytes[3] == RiffMagic[3];

                bool isWebp = bytes[8] == WebpMagic[0] &&
                              bytes[9] == WebpMagic[1] &&
                              bytes[10] == WebpMagic[2] &&
                              bytes[11] == WebpMagic[3];

                return isRiff && isWebp;

            default:
                return false;
        }
    }
}
