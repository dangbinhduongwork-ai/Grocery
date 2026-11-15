namespace GroceryHub.Modules.Catalog.Contracts;

public record CatalogProductContractDto(
    Guid Id,
    string Sku,
    string Name,
    string Slug,
    string Unit,
    long ListPrice,
    long SellingPrice,
    string Status,
    string? PrimaryImageUrl);

public interface ICatalogModuleContract
{
    Task<CatalogProductContractDto?> GetProductByIdAsync(Guid productId, CancellationToken ct = default);

    Task<bool> IsProductActiveAsync(Guid productId, CancellationToken ct = default);

    Task<IReadOnlyList<CatalogProductContractDto>> GetProductsByIdsAsync(IEnumerable<Guid> productIds, CancellationToken ct = default);
}
