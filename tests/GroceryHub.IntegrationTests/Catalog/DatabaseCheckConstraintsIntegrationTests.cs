using GroceryHub.Persistence;
using GroceryHub.Persistence.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GroceryHub.IntegrationTests.Catalog;

[Collection(SqlServerIntegrationCollection.CollectionName)]
public sealed class DatabaseCheckConstraintsIntegrationTests(SqlServerTestDatabaseFixture dbFixture) : IAsyncLifetime
{
    private readonly SqlServerTestDatabaseFixture _dbFixture = dbFixture;

    public async Task InitializeAsync()
    {
        await _dbFixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DirectSql_SellingPriceGreaterThanListPrice_ThrowsSqlExceptionCitingConstraint()
    {
        Guid categoryId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();

        await using GroceryHubDbContext db = _dbFixture.CreateDbContext();

        Category category = new()
        {
            Id = categoryId,
            Name = "Constraint Test Category",
            Slug = $"constraint-cat-{Guid.NewGuid():N}",
            SortOrder = 1,
            IsActive = true
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        // Attempt direct SQL INSERT where SellingPrice (100,000) > ListPrice (50,000)
        string insertSql = """
            INSERT INTO [catalog].[Products] (
                [Id], [Sku], [Name], [Slug], [SearchText], [CategoryId], [Unit],
                [ListPrice], [SellingPrice], [CostPrice], [Status], [IsFeatured],
                [RatingAvg], [RatingCount], [SoldCount], [CreatedAt]
            ) VALUES (
                @ProductId, @Sku, @Name, @Slug, @SearchText, @CategoryId, @Unit,
                @ListPrice, @SellingPrice, @CostPrice, @Status, 0,
                0.00, 0, 0, SYSDATETIMEOFFSET()
            );
            """;

        SqlException ex = await Assert.ThrowsAsync<SqlException>(async () =>
        {
            await db.Database.ExecuteSqlRawAsync(
                insertSql,
                new SqlParameter("@ProductId", productId),
                new SqlParameter("@Sku", "SKU-CK-PRICE-FAIL"),
                new SqlParameter("@Name", "Invalid Price Product"),
                new SqlParameter("@Slug", "invalid-price-product"),
                new SqlParameter("@SearchText", "invalid price product"),
                new SqlParameter("@CategoryId", categoryId),
                new SqlParameter("@Unit", "Gói"),
                new SqlParameter("@ListPrice", 50000L),
                new SqlParameter("@SellingPrice", 100000L),
                new SqlParameter("@CostPrice", 30000L),
                new SqlParameter("@Status", "Draft"));
        });

        Assert.Equal(547, ex.Number); // SQL Server error 547 = CHECK constraint violation
        Assert.Contains("CK_Products_SellingPrice_ListPrice", ex.Message);
        Assert.Contains("catalog.Products", ex.Message);
    }

    [Fact]
    public async Task DirectSql_NegativeOnHand_ThrowsSqlExceptionCitingStockInvariantsConstraint()
    {
        Guid categoryId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();

        await using GroceryHubDbContext db = _dbFixture.CreateDbContext();

        Category category = new()
        {
            Id = categoryId,
            Name = "Constraint Stock Category",
            Slug = $"constraint-stock-cat-{Guid.NewGuid():N}",
            SortOrder = 1,
            IsActive = true
        };
        Product product = new()
        {
            Id = productId,
            Sku = $"SKU-CK-STOCK-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            Name = "Stock Invariant Product",
            Slug = $"stock-invariant-product-{Guid.NewGuid():N}",
            SearchText = "stock invariant product",
            CategoryId = categoryId,
            Unit = "Chai",
            ListPrice = 40000,
            SellingPrice = 35000,
            CostPrice = 25000,
            Status = ProductStatus.Active
        };

        db.Categories.Add(category);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        // Attempt direct SQL INSERT into [inventory].[StockItems] with OnHand = -1
        string insertSql = """
            INSERT INTO [inventory].[StockItems] (
                [ProductId], [OnHand], [Reserved], [ReorderLevel], [CreatedAt]
            ) VALUES (
                @ProductId, -1, 0, 10, SYSDATETIMEOFFSET()
            );
            """;

        SqlException ex = await Assert.ThrowsAsync<SqlException>(async () =>
        {
            await db.Database.ExecuteSqlRawAsync(
                insertSql,
                new SqlParameter("@ProductId", productId));
        });

        Assert.Equal(547, ex.Number);
        Assert.Contains("CK_StockItems_Invariants", ex.Message);
        Assert.Contains("inventory.StockItems", ex.Message);
    }

    [Fact]
    public async Task DirectSql_ReservedExceedingOnHand_ThrowsSqlExceptionCitingStockInvariantsConstraint()
    {
        Guid categoryId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();

        await using GroceryHubDbContext db = _dbFixture.CreateDbContext();

        Category category = new()
        {
            Id = categoryId,
            Name = "Constraint Reserved Category",
            Slug = $"constraint-res-cat-{Guid.NewGuid():N}",
            SortOrder = 1,
            IsActive = true
        };
        Product product = new()
        {
            Id = productId,
            Sku = $"SKU-CK-RES-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            Name = "Reserved Greater Than OnHand Product",
            Slug = $"reserved-gt-onhand-product-{Guid.NewGuid():N}",
            SearchText = "reserved greater than onhand product",
            CategoryId = categoryId,
            Unit = "Chai",
            ListPrice = 40000,
            SellingPrice = 35000,
            CostPrice = 25000,
            Status = ProductStatus.Active
        };

        db.Categories.Add(category);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        // Attempt direct SQL INSERT with OnHand = 5, Reserved = 10 (Reserved > OnHand)
        string insertSql = """
            INSERT INTO [inventory].[StockItems] (
                [ProductId], [OnHand], [Reserved], [ReorderLevel], [CreatedAt]
            ) VALUES (
                @ProductId, 5, 10, 10, SYSDATETIMEOFFSET()
            );
            """;

        SqlException ex = await Assert.ThrowsAsync<SqlException>(async () =>
        {
            await db.Database.ExecuteSqlRawAsync(
                insertSql,
                new SqlParameter("@ProductId", productId));
        });

        Assert.Equal(547, ex.Number);
        Assert.Contains("CK_StockItems_Invariants", ex.Message);
        Assert.Contains("inventory.StockItems", ex.Message);
    }

    [Fact]
    public async Task DirectSql_UpdateViolatingCheckConstraints_ThrowsSqlExceptionCitingConstraint()
    {
        Guid categoryId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();

        await using GroceryHubDbContext db = _dbFixture.CreateDbContext();

        Category category = new()
        {
            Id = categoryId,
            Name = "Constraint Update Category",
            Slug = $"constraint-upd-cat-{Guid.NewGuid():N}",
            SortOrder = 1,
            IsActive = true
        };
        Product product = new()
        {
            Id = productId,
            Sku = $"SKU-CK-UPD-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            Name = "Update Constraint Product",
            Slug = $"update-constraint-product-{Guid.NewGuid():N}",
            SearchText = "update constraint product",
            CategoryId = categoryId,
            Unit = "Hộp",
            ListPrice = 100000,
            SellingPrice = 90000,
            CostPrice = 70000,
            Status = ProductStatus.Active
        };
        StockItem stockItem = new()
        {
            ProductId = productId,
            OnHand = 20,
            Reserved = 5,
            ReorderLevel = 10
        };

        db.Categories.Add(category);
        db.Products.Add(product);
        db.StockItems.Add(stockItem);
        await db.SaveChangesAsync();

        // 1. UPDATE [catalog].[Products] SET [SellingPrice] = [ListPrice] + 1
        SqlException exPrice = await Assert.ThrowsAsync<SqlException>(async () =>
        {
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE [catalog].[Products] SET [SellingPrice] = [ListPrice] + 1000 WHERE [Id] = @ProductId",
                new SqlParameter("@ProductId", productId));
        });
        Assert.Equal(547, exPrice.Number);
        Assert.Contains("CK_Products_SellingPrice_ListPrice", exPrice.Message);

        // 2. UPDATE [inventory].[StockItems] SET [OnHand] = -5
        SqlException exOnHand = await Assert.ThrowsAsync<SqlException>(async () =>
        {
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE [inventory].[StockItems] SET [OnHand] = -5 WHERE [ProductId] = @ProductId",
                new SqlParameter("@ProductId", productId));
        });
        Assert.Equal(547, exOnHand.Number);
        Assert.Contains("CK_StockItems_Invariants", exOnHand.Message);

        // 3. UPDATE [inventory].[StockItems] SET [Reserved] = [OnHand] + 1
        SqlException exReserved = await Assert.ThrowsAsync<SqlException>(async () =>
        {
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE [inventory].[StockItems] SET [Reserved] = [OnHand] + 1 WHERE [ProductId] = @ProductId",
                new SqlParameter("@ProductId", productId));
        });
        Assert.Equal(547, exReserved.Number);
        Assert.Contains("CK_StockItems_Invariants", exReserved.Message);
    }
}
