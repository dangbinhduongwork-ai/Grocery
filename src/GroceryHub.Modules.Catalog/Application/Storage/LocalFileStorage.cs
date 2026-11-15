using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GroceryHub.Modules.Catalog.Application.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _storageRoot;
    private readonly string _publicBaseUrl;

    public LocalFileStorage(IOptions<FileStorageOptions> options, IHostEnvironment? environment = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        FileStorageOptions opt = options.Value ?? new FileStorageOptions();

        string root = opt.LocalDirectory;
        if (!Path.IsPathRooted(root))
        {
            string contentRoot = environment?.ContentRootPath ?? Directory.GetCurrentDirectory();
            root = Path.GetFullPath(Path.Combine(contentRoot, root));
        }

        _storageRoot = root;
        _publicBaseUrl = opt.PublicBaseUrl?.TrimEnd('/') ?? "/api/v1/catalog/images";

        if (!Directory.Exists(_storageRoot))
        {
            Directory.CreateDirectory(_storageRoot);
        }
    }

    public async Task<string> SaveAsync(Stream stream, string filename, string contentType, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(filename);

        string safeName = Path.GetFileName(filename);
        string fullPath = Path.Combine(_storageRoot, safeName);

        // Ensure full path is within storage root
        string normalizedFullPath = Path.GetFullPath(fullPath);
        if (!normalizedFullPath.StartsWith(_storageRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid storage filename or path traversal detected.");
        }

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        await using FileStream fileStream = new(normalizedFullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await stream.CopyToAsync(fileStream, ct);

        return safeName;
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return Task.FromResult<Stream?>(null);
        }

        string safeName = Path.GetFileName(storageKey);
        string fullPath = Path.Combine(_storageRoot, safeName);
        string normalizedFullPath = Path.GetFullPath(fullPath);

        if (!normalizedFullPath.StartsWith(_storageRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(normalizedFullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream fileStream = new FileStream(normalizedFullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return Task.FromResult<Stream?>(fileStream);
    }

    public Task<bool> DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return Task.FromResult(false);
        }

        string safeName = Path.GetFileName(storageKey);
        string fullPath = Path.Combine(_storageRoot, safeName);
        string normalizedFullPath = Path.GetFullPath(fullPath);

        if (!normalizedFullPath.StartsWith(_storageRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(normalizedFullPath))
        {
            return Task.FromResult(false);
        }

        File.Delete(normalizedFullPath);
        return Task.FromResult(true);
    }

    public string GetPublicUrl(string storageKey)
    {
        string safeName = Path.GetFileName(storageKey ?? string.Empty);
        return $"{_publicBaseUrl}/{Uri.EscapeDataString(safeName)}";
    }
}
