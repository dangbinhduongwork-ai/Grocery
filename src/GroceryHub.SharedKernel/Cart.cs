namespace GroceryHub.SharedKernel;

public sealed record CartItemViewDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    string Name,
    string Unit,
    string? PackSize,
    long UnitPrice,
    long ListPrice,
    int Quantity,
    long LineSubtotal,
    bool InStock,
    bool LowStock,
    int AvailableQuantity,
    string? ImageUrl);

public sealed record CartWarningDto(
    string Code,
    string Message,
    Guid? ProductId = null);

public sealed record CartViewDto(
    Guid Id,
    IReadOnlyList<CartItemViewDto> Items,
    long Subtotal,
    string? CouponCode,
    long CouponDiscount,
    long EstimatedTotal,
    int MaxQuantityPerLine,
    IReadOnlyList<CartWarningDto> Warnings);

public sealed record AddCartItemRequest(
    Guid ProductId,
    int Quantity);

public sealed record UpdateCartItemRequest(
    int Quantity);

public sealed record ApplyCouponRequest(
    string Code);

public interface ICartMergeService
{
    Task<Result> MergeGuestCartOnLoginAsync(
        Guid userId,
        string guestToken,
        CancellationToken ct = default);
}

public interface ICartService
{
    Task<Result<(CartViewDto CartView, string? NewGuestToken)>> GetOrCreateCartViewAsync(
        Guid? userId,
        string? guestToken,
        CancellationToken ct = default);

    Task<Result<(CartViewDto CartView, string? NewGuestToken)>> AddItemAsync(
        Guid? userId,
        string? guestToken,
        AddCartItemRequest request,
        CancellationToken ct = default);

    Task<Result<CartViewDto>> UpdateItemQuantityAsync(
        Guid? userId,
        string? guestToken,
        Guid itemOrProductId,
        int newQuantity,
        CancellationToken ct = default);

    Task<Result<CartViewDto>> RemoveItemAsync(
        Guid? userId,
        string? guestToken,
        Guid itemOrProductId,
        CancellationToken ct = default);

    Task<Result> ClearCartAsync(
        Guid? userId,
        string? guestToken,
        CancellationToken ct = default);

    Task<Result<CartViewDto>> ApplyCouponAsync(
        Guid userId,
        string? guestToken,
        string couponCode,
        CancellationToken ct = default);

    Task<Result<CartViewDto>> RemoveCouponAsync(
        Guid userId,
        string? guestToken,
        CancellationToken ct = default);
}
