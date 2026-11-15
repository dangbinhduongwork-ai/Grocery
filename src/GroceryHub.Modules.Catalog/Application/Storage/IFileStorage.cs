namespace GroceryHub.Modules.Catalog.Application.Storage;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream stream, string filename, string contentType, CancellationToken ct = default);

    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct = default);

    Task<bool> DeleteAsync(string storageKey, CancellationToken ct = default);

    string GetPublicUrl(string storageKey);
}
