using GroceryHub.SharedKernel;

namespace GroceryHub.Modules.Catalog.Application.Storage;

public record SanitizedImageResult(
    string StorageKey,
    string Url,
    int Width,
    int Height,
    string ContentType);

public interface IImageUploadService
{
    Task<Result<SanitizedImageResult>> ProcessAndStoreImageAsync(
        Stream inputStream,
        string claimedMimeType,
        long reportedLength,
        CancellationToken ct = default);

    Task<bool> DeleteStoredImageAsync(string storageKey, CancellationToken ct = default);
}
