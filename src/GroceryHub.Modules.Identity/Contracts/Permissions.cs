namespace GroceryHub.Modules.Identity.Contracts;

public static class Permissions
{
    public const string CatalogRead = "catalog.read";
    public const string CatalogWrite = "catalog.write";
    public const string CatalogPriceWrite = "catalog.price.write";
    public const string CatalogCostRead = "catalog.cost.read";
    public const string InventoryRead = "inventory.read";
    public const string InventoryAdjust = "inventory.adjust";
    public const string PurchasingRead = "purchasing.read";
    public const string PurchasingWrite = "purchasing.write";
    public const string PurchasingReceive = "purchasing.receive";
    public const string OrdersRead = "orders.read";
    public const string OrdersProcess = "orders.process";
    public const string OrdersShip = "orders.ship";
    public const string OrdersCancel = "orders.cancel";
    public const string CustomersRead = "customers.read";
    public const string CustomersLock = "customers.lock";
    public const string StaffRead = "staff.read";
    public const string StaffCreate = "staff.create";
    public const string StaffManage = "staff.manage";
    public const string RolesManage = "roles.manage";
    public const string PaymentsRead = "payments.read";
    public const string PaymentsReview = "payments.review";
    public const string RefundsRequest = "refunds.request";
    public const string RefundsApprove = "refunds.approve";
    public const string PromotionsManage = "promotions.manage";
    public const string ReviewsModerate = "reviews.moderate";
    public const string ReportsRevenue = "reports.revenue";
    public const string ReportsProfit = "reports.profit";
    public const string AuditRead = "audit.read";
    public const string SettingsManage = "settings.manage";

    public static readonly IReadOnlyList<string> All =
    [
        CatalogRead, CatalogWrite, CatalogPriceWrite, CatalogCostRead,
        InventoryRead, InventoryAdjust, PurchasingRead, PurchasingWrite, PurchasingReceive,
        OrdersRead, OrdersProcess, OrdersShip, OrdersCancel,
        CustomersRead, CustomersLock, StaffRead, StaffCreate, StaffManage,
        RolesManage, PaymentsRead, PaymentsReview, RefundsRequest, RefundsApprove,
        PromotionsManage, ReviewsModerate, ReportsRevenue, ReportsProfit,
        AuditRead, SettingsManage
    ];
}
