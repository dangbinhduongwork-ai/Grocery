using GroceryHub.Modules.Reporting.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace GroceryHub.Modules.Reporting.Api;

internal static class ReportingEndpoints
{
    public static IEndpointRouteBuilder MapReportingApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/admin/reports")
            .WithTags("Admin Reporting");

        // GET /api/v1/admin/reports/dashboard
        group.MapGet("/dashboard", async (
            ReportingService reportingService,
            CancellationToken ct) =>
        {
            var kpis = await reportingService.GetDashboardKpiAsync(ct);
            return TypedResults.Ok(kpis);
        })
        .RequireAuthorization("reports.revenue")
        .WithName("GetDashboardKpis")
        .WithSummary("Get admin dashboard overview KPIs");

        // GET /api/v1/admin/reports/revenue
        group.MapGet("/revenue", async (
            ReportingService reportingService,
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            [FromQuery] string? granularity,
            [FromQuery] string? format,
            CancellationToken ct) =>
        {
            var report = await reportingService.GetRevenueReportAsync(from, to, granularity, ct);

            if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                var headers = new[] { "Date", "NetRevenue", "OrdersCount" };
                var rows = report.Points.Select(p => (IReadOnlyList<string>)[
                    p.Date,
                    p.NetRevenue.ToString(),
                    p.OrdersCount.ToString()
                ]);

                byte[] csvBytes = CsvExporter.BuildCsv(headers, rows);
                return Results.File(csvBytes, "text/csv; charset=utf-8", "revenue-report.csv");
            }

            return Results.Ok(report);
        })
        .RequireAuthorization("reports.revenue")
        .WithName("GetRevenueReport")
        .WithSummary("Get net sales revenue report");

        // GET /api/v1/admin/reports/top-products
        group.MapGet("/top-products", async (
            ReportingService reportingService,
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            [FromQuery] int? limit,
            [FromQuery] string? format,
            CancellationToken ct) =>
        {
            var products = await reportingService.GetTopProductsReportAsync(from, to, limit ?? 20, ct);

            if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                var headers = new[] { "Sku", "ProductName", "Category", "UnitsSold", "NetRevenue" };
                var rows = products.Select(p => (IReadOnlyList<string>)[
                    p.Sku,
                    p.ProductName,
                    p.CategoryName,
                    p.UnitsSold.ToString(),
                    p.NetRevenue.ToString()
                ]);

                byte[] csvBytes = CsvExporter.BuildCsv(headers, rows);
                return Results.File(csvBytes, "text/csv; charset=utf-8", "top-products.csv");
            }

            return Results.Ok(products);
        })
        .RequireAuthorization("reports.revenue")
        .WithName("GetTopProductsReport")
        .WithSummary("Get top selling products report");

        // GET /api/v1/admin/reports/profit
        group.MapGet("/profit", async (
            ReportingService reportingService,
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            [FromQuery] string? granularity,
            [FromQuery] string? format,
            CancellationToken ct) =>
        {
            var report = await reportingService.GetProfitReportAsync(from, to, granularity, ct);

            if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                var headers = new[] { "Date", "NetRevenue", "COGS", "GrossProfit", "MarginPercent" };
                var rows = report.Series.Select(s => (IReadOnlyList<string>)[
                    s.Date,
                    s.NetRevenue.ToString(),
                    s.Cogs.ToString(),
                    s.GrossProfit.ToString(),
                    s.MarginPercent.ToString("F2")
                ]);

                byte[] csvBytes = CsvExporter.BuildCsv(headers, rows);
                return Results.File(csvBytes, "text/csv; charset=utf-8", "profit-report.csv");
            }

            return Results.Ok(report);
        })
        .RequireAuthorization("reports.profit")
        .WithName("GetProfitReport")
        .WithSummary("Get gross profit and COGS report");

        // GET /api/v1/admin/reports/stock-valuation
        group.MapGet("/stock-valuation", async (
            ReportingService reportingService,
            [FromQuery] Guid? categoryId,
            [FromQuery] string? format,
            CancellationToken ct) =>
        {
            var report = await reportingService.GetStockValuationReportAsync(categoryId, ct);

            if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                var headers = new[] { "Sku", "ProductName", "Category", "OnHand", "CostPrice", "TotalValue" };
                var rows = report.Items.Select(i => (IReadOnlyList<string>)[
                    i.Sku,
                    i.Name,
                    i.CategoryName,
                    i.OnHand.ToString(),
                    i.CostPrice.ToString(),
                    i.TotalValue.ToString()
                ]);

                byte[] csvBytes = CsvExporter.BuildCsv(headers, rows);
                return Results.File(csvBytes, "text/csv; charset=utf-8", "stock-valuation.csv");
            }

            return Results.Ok(report);
        })
        .RequireAuthorization("reports.profit")
        .WithName("GetStockValuationReport")
        .WithSummary("Get current inventory stock valuation");

        return endpoints;
    }
}
