namespace GroceryHub.Modules.Reporting.Contracts;

public sealed record DashboardKpiDto(
    long TodayRevenue,
    long Last7DaysRevenue,
    long Last30DaysRevenue,
    IReadOnlyDictionary<string, int> OrdersByStatus,
    int LowStockCount,
    int PendingReviewsCount,
    int PaymentAnomaliesCount,
    IReadOnlyList<TopProductReportItemDto> TopProducts);

public sealed record RevenuePointDto(
    string Date,
    long NetRevenue,
    int OrdersCount);

public sealed record RevenueReportDto(
    DateTimeOffset PeriodFrom,
    DateTimeOffset PeriodTo,
    string Granularity,
    long TotalNetRevenue,
    long ShippingFeeTotal,
    long RefundsTotal,
    IReadOnlyList<RevenuePointDto> Points);

public sealed record TopProductReportItemDto(
    Guid ProductId,
    string Sku,
    string ProductName,
    string CategoryName,
    int UnitsSold,
    long NetRevenue);

public sealed record ProfitPointDto(
    string Date,
    long NetRevenue,
    long Cogs,
    long GrossProfit,
    decimal MarginPercent);

public sealed record ProfitReportDto(
    DateTimeOffset PeriodFrom,
    DateTimeOffset PeriodTo,
    string Granularity,
    long TotalNetRevenue,
    long TotalCogsSnapshot,
    long GrossProfit,
    decimal GrossMarginPercent,
    IReadOnlyList<ProfitPointDto> Series);

public sealed record StockValuationItemDto(
    Guid ProductId,
    string Sku,
    string Name,
    string CategoryName,
    int OnHand,
    long CostPrice,
    long TotalValue);

public sealed record StockValuationReportDto(
    int TotalOnHandUnits,
    long TotalInventoryValue,
    IReadOnlyList<StockValuationItemDto> Items);
