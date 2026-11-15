namespace GroceryHub.Modules.Catalog.Application.Storage;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string LocalDirectory { get; set; } = "uploads/products";

    public string PublicBaseUrl { get; set; } = "/api/v1/catalog/images";

    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024; // 5 MB
}
