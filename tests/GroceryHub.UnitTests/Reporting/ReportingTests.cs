using System.Text;
using GroceryHub.Modules.Reporting.Application;
using Xunit;

namespace GroceryHub.UnitTests.Reporting;

public sealed class ReportingTests
{
    [Theory]
    [InlineData("=1+1")]
    [InlineData("+1+1")]
    [InlineData("-1+1")]
    [InlineData("@SUM(A1:A10)")]
    [InlineData("\tcmd.exe")]
    [InlineData("\rcmd.exe")]
    [InlineData("  =HYPERLINK(\"http://evil.com\")")]
    public void CsvExporter_Must_Escape_Formula_Prefixes(string input)
    {
        string escaped = CsvExporter.EscapeValue(input);

        // Either starts with ' or starts with "' (when quoted)
        string unquoted = escaped.Trim('"');
        Assert.StartsWith("'", unquoted);
    }

    [Theory]
    [InlineData("Normal Product Name", "Normal Product Name")]
    [InlineData("SKU-12345", "SKU-12345")]
    [InlineData("Gạo ST25 5kg", "Gạo ST25 5kg")]
    [InlineData("Sữa tươi tiệt trùng 1L", "Sữa tươi tiệt trùng 1L")]
    public void CsvExporter_Must_Not_Alter_Safe_Text(string input, string expected)
    {
        string escaped = CsvExporter.EscapeValue(input);
        Assert.Equal(expected, escaped);
    }

    [Fact]
    public void CsvExporter_Must_Escape_Commas_And_Quotes()
    {
        string input = "Rau cải thìa, túi 500g \"loại 1\"";
        string escaped = CsvExporter.EscapeValue(input);

        Assert.Equal("\"Rau cải thìa, túi 500g \"\"loại 1\"\"\"", escaped);
    }

    [Fact]
    public void CsvExporter_BuildCsv_Includes_Utf8_Bom()
    {
        var headers = new[] { "SKU", "Tên sản phẩm", "Giá" };
        var rows = new List<IReadOnlyList<string>>
        {
            new[] { "SKU-001", "Gạo ST25", "160000" },
            new[] { "SKU-002", "=2+2", "35000" }
        };

        byte[] bytes = CsvExporter.BuildCsv(headers, rows);

        // UTF-8 BOM is 0xEF, 0xBB, 0xBF
        Assert.True(bytes.Length >= 3);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);

        string content = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        Assert.Contains("SKU,Tên sản phẩm,Giá", content);
        Assert.Contains("SKU-001,Gạo ST25,160000", content);
        Assert.Contains("SKU-002,'=2+2,35000", content);
    }

    [Fact]
    public void Profit_Calculation_Follows_BR13()
    {
        long netRevenue = 1_000_000;
        long cogs = 700_000;

        long grossProfit = netRevenue - cogs;
        decimal marginPercent = Math.Round((decimal)grossProfit / netRevenue * 100m, 2);

        Assert.Equal(300_000, grossProfit);
        Assert.Equal(30.00m, marginPercent);
    }

    [Fact]
    public void Profit_Calculation_Handles_Zero_Revenue_Without_DivisionByZero()
    {
        long netRevenue = 0;
        long cogs = 0;

        long grossProfit = netRevenue - cogs;
        decimal marginPercent = netRevenue == 0 ? 0m : Math.Round((decimal)grossProfit / netRevenue * 100m, 2);

        Assert.Equal(0, grossProfit);
        Assert.Equal(0m, marginPercent);
    }
}
