namespace GroceryHub.Persistence;

public static class DatabaseSchemas
{
    public const string Identity = "identity";
    public const string Customers = "customers";
    public const string Catalog = "catalog";
    public const string Inventory = "inventory";
    public const string Purchasing = "purchasing";
    public const string Promotions = "promotions";
    public const string Cart = "cart";
    public const string Ordering = "ordering";
    public const string Payments = "payments";
    public const string Reviews = "reviews";
    public const string Audit = "audit";
    public const string Shared = "shared";

    public static readonly IReadOnlyList<string> All =
    [
        Identity,
        Customers,
        Catalog,
        Inventory,
        Purchasing,
        Promotions,
        Cart,
        Ordering,
        Payments,
        Reviews,
        Audit,
        Shared,
    ];
}
