using GroceryHub.Modules.Reporting.Contracts;
using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace GroceryHub.Modules.Reporting.Application;

public sealed class ReportingService
{
    private readonly GroceryHubDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private static readonly TimeZoneInfo VietnamTimeZone = GetVietnamTimeZone();

    public ReportingService(GroceryHubDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }
    }

    private static DateTime ToVnDate(DateTimeOffset dto)
    {
        return TimeZoneInfo.ConvertTime(dto, VietnamTimeZone).Date;
    }

    private static DateTimeOffset ToVnStartOfDay(DateTime date)
    {
        var localDateTime = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var offset = VietnamTimeZone.GetUtcOffset(localDateTime);
        return new DateTimeOffset(localDateTime, offset);
    }

    public async Task<DashboardKpiDto> GetDashboardKpiAsync(CancellationToken ct = default)
    {
        DateTime nowVn = TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), VietnamTimeZone).Date;
        DateTimeOffset todayStart = ToVnStartOfDay(nowVn);
        DateTimeOffset sevenDaysAgo = ToVnStartOfDay(nowVn.AddDays(-6));
        DateTimeOffset thirtyDaysAgo = ToVnStartOfDay(nowVn.AddDays(-29));

        // Group orders count by status
        var statusCounts = await _dbContext.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var ordersByStatus = OrderStatusConstants.All.ToDictionary(
            status => status,
            status => statusCounts.FirstOrDefault(x => x.Status == status)?.Count ?? 0,
            StringComparer.OrdinalIgnoreCase);

        // Fetch delivered orders
        var deliveredOrders = await _dbContext.Orders
            .Where(o => o.Status == OrderStatusConstants.Delivered)
            .Select(o => new
            {
                o.Id,
                DeliveredDate = o.StatusHistory
                    .Where(h => h.ToStatus == OrderStatusConstants.Delivered)
                    .OrderByDescending(h => h.CreatedAt)
                    .Select(h => (DateTimeOffset?)h.CreatedAt)
                    .FirstOrDefault() ?? o.UpdatedAt ?? o.CreatedAt,
                NetRevenue = o.Subtotal - o.CouponDiscount
            })
            .ToListAsync(ct);

        long todayRevenue = deliveredOrders
            .Where(o => o.DeliveredDate >= todayStart)
            .Sum(o => o.NetRevenue);

        long last7DaysRevenue = deliveredOrders
            .Where(o => o.DeliveredDate >= sevenDaysAgo)
            .Sum(o => o.NetRevenue);

        long last30DaysRevenue = deliveredOrders
            .Where(o => o.DeliveredDate >= thirtyDaysAgo)
            .Sum(o => o.NetRevenue);

        // Low stock count
        int lowStockCount = await _dbContext.StockItems
            .CountAsync(s => (s.OnHand - s.Reserved) <= s.ReorderLevel, ct);

        // Pending reviews count
        int pendingReviewsCount = await _dbContext.Reviews
            .CountAsync(r => r.Status == ReviewStatus.Pending, ct);

        int paymentAnomaliesCount = 0; // Phase 10

        // Top 5 products
        var topProducts = await GetTopProductsReportAsync(thirtyDaysAgo, _timeProvider.GetUtcNow(), 5, ct);

        return new DashboardKpiDto(
            todayRevenue,
            last7DaysRevenue,
            last30DaysRevenue,
            ordersByStatus,
            lowStockCount,
            pendingReviewsCount,
            paymentAnomaliesCount,
            topProducts);
    }

    public async Task<RevenueReportDto> GetRevenueReportAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? granularity,
        CancellationToken ct = default)
    {
        DateTimeOffset start = from ?? _timeProvider.GetUtcNow().AddDays(-30);
        DateTimeOffset end = to ?? _timeProvider.GetUtcNow();
        string gran = string.Equals(granularity, "month", StringComparison.OrdinalIgnoreCase) ? "month" : "day";

        var deliveredOrders = await _dbContext.Orders
            .Where(o => o.Status == OrderStatusConstants.Delivered)
            .Select(o => new
            {
                o.Id,
                DeliveredDate = o.StatusHistory
                    .Where(h => h.ToStatus == OrderStatusConstants.Delivered)
                    .OrderByDescending(h => h.CreatedAt)
                    .Select(h => (DateTimeOffset?)h.CreatedAt)
                    .FirstOrDefault() ?? o.UpdatedAt ?? o.CreatedAt,
                NetRevenue = o.Subtotal - o.CouponDiscount,
                o.ShippingFee
            })
            .Where(o => o.DeliveredDate >= start && o.DeliveredDate <= end)
            .ToListAsync(ct);

        long totalNetRevenue = deliveredOrders.Sum(o => o.NetRevenue);
        long totalShipping = deliveredOrders.Sum(o => o.ShippingFee);
        long totalRefunds = 0;

        var points = deliveredOrders
            .GroupBy(o =>
            {
                DateTime vnDate = ToVnDate(o.DeliveredDate);
                return gran == "month" ? vnDate.ToString("yyyy-MM") : vnDate.ToString("yyyy-MM-dd");
            })
            .OrderBy(g => g.Key)
            .Select(g => new RevenuePointDto(
                g.Key,
                g.Sum(x => x.NetRevenue),
                g.Count()))
            .ToList();

        return new RevenueReportDto(
            start,
            end,
            gran,
            totalNetRevenue,
            totalShipping,
            totalRefunds,
            points);
    }

    public async Task<IReadOnlyList<TopProductReportItemDto>> GetTopProductsReportAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit = 20,
        CancellationToken ct = default)
    {
        DateTimeOffset start = from ?? _timeProvider.GetUtcNow().AddDays(-30);
        DateTimeOffset end = to ?? _timeProvider.GetUtcNow();

        var deliveredOrderIds = await _dbContext.Orders
            .Where(o => o.Status == OrderStatusConstants.Delivered)
            .Select(o => new
            {
                o.Id,
                DeliveredDate = o.StatusHistory
                    .Where(h => h.ToStatus == OrderStatusConstants.Delivered)
                    .OrderByDescending(h => h.CreatedAt)
                    .Select(h => (DateTimeOffset?)h.CreatedAt)
                    .FirstOrDefault() ?? o.UpdatedAt ?? o.CreatedAt
            })
            .Where(o => o.DeliveredDate >= start && o.DeliveredDate <= end)
            .Select(o => o.Id)
            .ToListAsync(ct);

        if (deliveredOrderIds.Count == 0)
        {
            return Array.Empty<TopProductReportItemDto>();
        }

        var productStats = await _dbContext.OrderItems
            .Where(i => deliveredOrderIds.Contains(i.OrderId))
            .GroupBy(i => i.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                UnitsSold = g.Sum(x => x.Quantity),
                NetRevenue = g.Sum(x => (long)x.Quantity * x.UnitPrice - x.DiscountAllocated)
            })
            .OrderByDescending(x => x.UnitsSold)
            .Take(limit)
            .ToListAsync(ct);

        var productIds = productStats.Select(p => p.ProductId).ToList();

        var products = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var result = new List<TopProductReportItemDto>();
        foreach (var stat in productStats)
        {
            if (products.TryGetValue(stat.ProductId, out var product))
            {
                result.Add(new TopProductReportItemDto(
                    stat.ProductId,
                    product.Sku,
                    product.Name,
                    product.Category?.Name ?? "N/A",
                    stat.UnitsSold,
                    stat.NetRevenue));
            }
        }

        return result;
    }

    public async Task<ProfitReportDto> GetProfitReportAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? granularity,
        CancellationToken ct = default)
    {
        DateTimeOffset start = from ?? _timeProvider.GetUtcNow().AddDays(-30);
        DateTimeOffset end = to ?? _timeProvider.GetUtcNow();
        string gran = string.Equals(granularity, "month", StringComparison.OrdinalIgnoreCase) ? "month" : "day";

        var deliveredOrders = await _dbContext.Orders
            .Where(o => o.Status == OrderStatusConstants.Delivered)
            .Select(o => new
            {
                o.Id,
                DeliveredDate = o.StatusHistory
                    .Where(h => h.ToStatus == OrderStatusConstants.Delivered)
                    .OrderByDescending(h => h.CreatedAt)
                    .Select(h => (DateTimeOffset?)h.CreatedAt)
                    .FirstOrDefault() ?? o.UpdatedAt ?? o.CreatedAt,
                NetRevenue = o.Subtotal - o.CouponDiscount,
                Cogs = o.Items.Sum(i => (long)i.Quantity * i.UnitCostSnapshot)
            })
            .Where(o => o.DeliveredDate >= start && o.DeliveredDate <= end)
            .ToListAsync(ct);

        long totalNetRevenue = deliveredOrders.Sum(o => o.NetRevenue);
        long totalCogs = deliveredOrders.Sum(o => o.Cogs);
        long grossProfit = totalNetRevenue - totalCogs;
        decimal grossMarginPercent = totalNetRevenue == 0
            ? 0m
            : Math.Round((decimal)grossProfit / totalNetRevenue * 100m, 2);

        var series = deliveredOrders
            .GroupBy(o =>
            {
                DateTime vnDate = ToVnDate(o.DeliveredDate);
                return gran == "month" ? vnDate.ToString("yyyy-MM") : vnDate.ToString("yyyy-MM-dd");
            })
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                long rev = g.Sum(x => x.NetRevenue);
                long cogs = g.Sum(x => x.Cogs);
                long profit = rev - cogs;
                decimal margin = rev == 0 ? 0m : Math.Round((decimal)profit / rev * 100m, 2);
                return new ProfitPointDto(g.Key, rev, cogs, profit, margin);
            })
            .ToList();

        return new ProfitReportDto(
            start,
            end,
            gran,
            totalNetRevenue,
            totalCogs,
            grossProfit,
            grossMarginPercent,
            series);
    }

    public async Task<StockValuationReportDto> GetStockValuationReportAsync(
        Guid? categoryId,
        CancellationToken ct = default)
    {
        var query = _dbContext.StockItems
            .AsNoTracking()
            .Join(
                _dbContext.Products.AsNoTracking().Where(p => p.DeletedAt == null),
                stock => stock.ProductId,
                product => product.Id,
                (stock, product) => new { stock, product });

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.product.CategoryId == categoryId.Value);
        }

        var list = await query
            .Select(x => new
            {
                x.product.Id,
                x.product.Sku,
                x.product.Name,
                CategoryName = x.product.Category != null ? x.product.Category.Name : "N/A",
                x.stock.OnHand,
                x.product.CostPrice
            })
            .ToListAsync(ct);

        int totalUnits = list.Sum(x => x.OnHand);
        long totalValue = list.Sum(x => (long)x.OnHand * x.CostPrice);

        var items = list
            .Select(x => new StockValuationItemDto(
                x.Id,
                x.Sku,
                x.Name,
                x.CategoryName,
                x.OnHand,
                x.CostPrice,
                (long)x.OnHand * x.CostPrice))
            .ToList();

        return new StockValuationReportDto(totalUnits, totalValue, items);
    }
}
