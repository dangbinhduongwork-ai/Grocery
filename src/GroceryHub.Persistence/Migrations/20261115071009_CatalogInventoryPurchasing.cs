using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroceryHub.Persistence.Migrations;

/// <inheritdoc />
public partial class CatalogInventoryPurchasing : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "catalog");

        migrationBuilder.EnsureSchema(
            name: "inventory");

        migrationBuilder.CreateTable(
            name: "Banners",
            schema: "catalog",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                Subtitle = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                ImageKey = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                LinkUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                Placement = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "HomeHero"),
                SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Banners", x => x.Id);
                table.CheckConstraint("CK_Banners_Schedule", "[StartsAt] IS NULL OR [EndsAt] IS NULL OR [StartsAt] <= [EndsAt]");
            });

        migrationBuilder.CreateTable(
            name: "Brands",
            schema: "catalog",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Slug = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                LogoKey = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "SYSDATETIMEOFFSET()"),
                DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Brands", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Categories",
            schema: "catalog",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Slug = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                ImageKey = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "SYSDATETIMEOFFSET()"),
                DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Categories", x => x.Id);
                table.CheckConstraint("CK_Categories_NoSelfParent", "[ParentId] IS NULL OR [ParentId] <> [Id]");
                table.ForeignKey(
                    name: "FK_Categories_Categories_ParentId",
                    column: x => x.ParentId,
                    principalSchema: "catalog",
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "StockItems",
            schema: "inventory",
            columns: table => new
            {
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OnHand = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                Reserved = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                ReorderLevel = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StockItems", x => x.ProductId);
                table.CheckConstraint("CK_StockItems_Invariants", "[OnHand] >= 0 AND [Reserved] >= 0 AND [Reserved] <= [OnHand]");
            });

        migrationBuilder.CreateTable(
            name: "StockMovements",
            schema: "inventory",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Type = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false),
                OnHandBefore = table.Column<int>(type: "int", nullable: false),
                OnHandAfter = table.Column<int>(type: "int", nullable: false),
                UnitCost = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                ReferenceType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StockMovements", x => x.Id);
                table.CheckConstraint("CK_StockMovements_Quantities", "[OnHandBefore] >= 0 AND [OnHandAfter] >= 0 AND [UnitCost] >= 0");
                table.CheckConstraint("CK_StockMovements_Type", "[Type] IN ('PurchaseReceipt', 'Sale', 'Adjustment', 'Return', 'Waste')");
            });

        migrationBuilder.CreateTable(
            name: "Suppliers",
            schema: "purchasing",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                ContactPerson = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                TaxCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Suppliers", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Products",
            schema: "catalog",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Sku = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                Barcode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                Slug = table.Column<string>(type: "nvarchar(280)", maxLength: 280, nullable: false),
                SearchText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                PackSize = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                ListPrice = table.Column<long>(type: "bigint", nullable: false),
                SellingPrice = table.Column<long>(type: "bigint", nullable: false),
                CostPrice = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false, defaultValue: "Draft"),
                ShortDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                SpecsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                IsFeatured = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                RatingAvg = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false, defaultValue: 0.00m),
                RatingCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                SoldCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                ExpiryTracked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "SYSDATETIMEOFFSET()"),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Products", x => x.Id);
                table.CheckConstraint("CK_Products_CostPrice", "[CostPrice] >= 0");
                table.CheckConstraint("CK_Products_ListPrice", "[ListPrice] >= 0");
                table.CheckConstraint("CK_Products_Rating", "[RatingAvg] >= 0.00 AND [RatingAvg] <= 5.00 AND [RatingCount] >= 0 AND [SoldCount] >= 0");
                table.CheckConstraint("CK_Products_SellingPrice", "[SellingPrice] >= 0");
                table.CheckConstraint("CK_Products_SellingPrice_ListPrice", "[SellingPrice] <= [ListPrice]");
                table.CheckConstraint("CK_Products_SpecsJson", "[SpecsJson] IS NULL OR ISJSON([SpecsJson]) = 1");
                table.CheckConstraint("CK_Products_Status", "[Status] IN ('Draft', 'Active', 'Archived')");
                table.ForeignKey(
                    name: "FK_Products_Brands_BrandId",
                    column: x => x.BrandId,
                    principalSchema: "catalog",
                    principalTable: "Brands",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Products_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalSchema: "catalog",
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "StockReservations",
            schema: "inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false, defaultValue: "Active"),
                ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StockReservations", x => x.Id);
                table.CheckConstraint("CK_StockReservations_Quantity", "[Quantity] > 0");
                table.CheckConstraint("CK_StockReservations_Status", "[Status] IN ('Active', 'Released', 'Committed')");
                table.ForeignKey(
                    name: "FK_StockReservations_StockItems_ProductId",
                    column: x => x.ProductId,
                    principalSchema: "inventory",
                    principalTable: "StockItems",
                    principalColumn: "ProductId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PurchaseOrders",
            schema: "purchasing",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PoNumber = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false, defaultValue: "Draft"),
                TotalAmount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "SYSDATETIMEOFFSET()"),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                table.CheckConstraint("CK_PurchaseOrders_Status", "[Status] IN ('Draft', 'Submitted', 'PartiallyReceived', 'Received', 'Closed', 'Cancelled')");
                table.CheckConstraint("CK_PurchaseOrders_TotalAmount", "[TotalAmount] >= 0");
                table.ForeignKey(
                    name: "FK_PurchaseOrders_Suppliers_SupplierId",
                    column: x => x.SupplierId,
                    principalSchema: "purchasing",
                    principalTable: "Suppliers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ProductImages",
            schema: "catalog",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                StorageKey = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                IsPrimary = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                AltText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Width = table.Column<int>(type: "int", nullable: false, defaultValue: 800),
                Height = table.Column<int>(type: "int", nullable: false, defaultValue: 800),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductImages", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProductImages_Products_ProductId",
                    column: x => x.ProductId,
                    principalSchema: "catalog",
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProductPriceHistory",
            schema: "catalog",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OldSellingPrice = table.Column<long>(type: "bigint", nullable: false),
                NewSellingPrice = table.Column<long>(type: "bigint", nullable: false),
                OldListPrice = table.Column<long>(type: "bigint", nullable: false),
                NewListPrice = table.Column<long>(type: "bigint", nullable: false),
                ChangedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductPriceHistory", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProductPriceHistory_Products_ProductId",
                    column: x => x.ProductId,
                    principalSchema: "catalog",
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "GoodsReceipts",
            schema: "purchasing",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ReceiptNumber = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ReceivedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GoodsReceipts", x => x.Id);
                table.ForeignKey(
                    name: "FK_GoodsReceipts_PurchaseOrders_PurchaseOrderId",
                    column: x => x.PurchaseOrderId,
                    principalSchema: "purchasing",
                    principalTable: "PurchaseOrders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PurchaseOrderItems",
            schema: "purchasing",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrderedQuantity = table.Column<int>(type: "int", nullable: false),
                ReceivedQuantity = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                UnitCost = table.Column<long>(type: "bigint", nullable: false),
                Subtotal = table.Column<long>(type: "bigint", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PurchaseOrderItems", x => x.Id);
                table.CheckConstraint("CK_PurchaseOrderItems_Quantities", "[OrderedQuantity] > 0 AND [ReceivedQuantity] >= 0 AND [UnitCost] >= 0 AND [Subtotal] >= 0");
                table.ForeignKey(
                    name: "FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId",
                    column: x => x.PurchaseOrderId,
                    principalSchema: "purchasing",
                    principalTable: "PurchaseOrders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "GoodsReceiptItems",
            schema: "purchasing",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                GoodsReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PurchaseOrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ReceivedQuantity = table.Column<int>(type: "int", nullable: false),
                UnitCost = table.Column<long>(type: "bigint", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GoodsReceiptItems", x => x.Id);
                table.CheckConstraint("CK_GoodsReceiptItems_Quantities", "[ReceivedQuantity] > 0 AND [UnitCost] >= 0");
                table.ForeignKey(
                    name: "FK_GoodsReceiptItems_GoodsReceipts_GoodsReceiptId",
                    column: x => x.GoodsReceiptId,
                    principalSchema: "purchasing",
                    principalTable: "GoodsReceipts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Banners_Placement_Active",
            schema: "catalog",
            table: "Banners",
            columns: new[] { "Placement", "IsActive", "SortOrder" })
            .Annotation("SqlServer:Include", new[] { "Title", "ImageKey", "LinkUrl", "StartsAt", "EndsAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Brands_IsActive_Name",
            schema: "catalog",
            table: "Brands",
            columns: new[] { "IsActive", "Name" },
            filter: "[DeletedAt] IS NULL");

        migrationBuilder.CreateIndex(
            name: "UQ_Brands_Name",
            schema: "catalog",
            table: "Brands",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UQ_Brands_Slug",
            schema: "catalog",
            table: "Brands",
            column: "Slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Categories_ParentId_SortOrder",
            schema: "catalog",
            table: "Categories",
            columns: new[] { "ParentId", "SortOrder" },
            filter: "[DeletedAt] IS NULL")
            .Annotation("SqlServer:Include", new[] { "Name", "Slug", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "UQ_Categories_Slug",
            schema: "catalog",
            table: "Categories",
            column: "Slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_GoodsReceiptItems_GoodsReceiptId",
            schema: "purchasing",
            table: "GoodsReceiptItems",
            column: "GoodsReceiptId");

        migrationBuilder.CreateIndex(
            name: "IX_GoodsReceiptItems_ProductId",
            schema: "purchasing",
            table: "GoodsReceiptItems",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_GoodsReceipts_PurchaseOrderId",
            schema: "purchasing",
            table: "GoodsReceipts",
            column: "PurchaseOrderId",
            filter: "[PurchaseOrderId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_GoodsReceipts_ReceivedAt",
            schema: "purchasing",
            table: "GoodsReceipts",
            column: "ReceivedAt");

        migrationBuilder.CreateIndex(
            name: "UQ_GoodsReceipts_ReceiptNumber",
            schema: "purchasing",
            table: "GoodsReceipts",
            column: "ReceiptNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProductImages_ProductId_SortOrder",
            schema: "catalog",
            table: "ProductImages",
            columns: new[] { "ProductId", "SortOrder" });

        migrationBuilder.CreateIndex(
            name: "UQ_ProductImages_ProductId_Primary",
            schema: "catalog",
            table: "ProductImages",
            column: "ProductId",
            unique: true,
            filter: "[IsPrimary] = 1");

        migrationBuilder.CreateIndex(
            name: "UQ_ProductImages_StorageKey",
            schema: "catalog",
            table: "ProductImages",
            column: "StorageKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProductPriceHistory_ProductId_ChangedAt",
            schema: "catalog",
            table: "ProductPriceHistory",
            columns: new[] { "ProductId", "ChangedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Products_Barcode",
            schema: "catalog",
            table: "Products",
            column: "Barcode",
            filter: "[Barcode] IS NOT NULL AND [DeletedAt] IS NULL")
            .Annotation("SqlServer:Include", new[] { "Sku", "Name", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_Products_BrandId_Status",
            schema: "catalog",
            table: "Products",
            columns: new[] { "BrandId", "Status" },
            filter: "[BrandId] IS NOT NULL AND [DeletedAt] IS NULL")
            .Annotation("SqlServer:Include", new[] { "CategoryId", "SellingPrice" });

        migrationBuilder.CreateIndex(
            name: "IX_Products_Category_Status_SellingPrice",
            schema: "catalog",
            table: "Products",
            columns: new[] { "CategoryId", "Status", "SellingPrice" },
            filter: "[DeletedAt] IS NULL")
            .Annotation("SqlServer:Include", new[] { "Name", "Slug", "Sku", "BrandId", "Unit", "PackSize", "ListPrice", "IsFeatured", "RatingAvg", "RatingCount", "SoldCount", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Products_SearchText",
            schema: "catalog",
            table: "Products",
            column: "SearchText",
            filter: "[Status] = 'Active' AND [DeletedAt] IS NULL")
            .Annotation("SqlServer:Include", new[] { "Id", "Name", "Slug", "CategoryId", "BrandId", "SellingPrice", "ListPrice", "Status", "SoldCount" });

        migrationBuilder.CreateIndex(
            name: "IX_Products_Status_SoldCount",
            schema: "catalog",
            table: "Products",
            columns: new[] { "Status", "SoldCount", "CreatedAt" },
            filter: "[DeletedAt] IS NULL")
            .Annotation("SqlServer:Include", new[] { "Name", "Slug", "CategoryId", "BrandId", "SellingPrice", "ListPrice", "Unit", "PackSize", "RatingAvg", "RatingCount" });

        migrationBuilder.CreateIndex(
            name: "UQ_Products_Sku",
            schema: "catalog",
            table: "Products",
            column: "Sku",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UQ_Products_Slug",
            schema: "catalog",
            table: "Products",
            column: "Slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PurchaseOrderItems_ProductId",
            schema: "purchasing",
            table: "PurchaseOrderItems",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "UQ_PurchaseOrderItems_PO_Product",
            schema: "purchasing",
            table: "PurchaseOrderItems",
            columns: new[] { "PurchaseOrderId", "ProductId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PurchaseOrders_Status_CreatedAt",
            schema: "purchasing",
            table: "PurchaseOrders",
            columns: new[] { "Status", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_PurchaseOrders_SupplierId_Status",
            schema: "purchasing",
            table: "PurchaseOrders",
            columns: new[] { "SupplierId", "Status", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "UQ_PurchaseOrders_PoNumber",
            schema: "purchasing",
            table: "PurchaseOrders",
            column: "PoNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_StockItems_LowStock",
            schema: "inventory",
            table: "StockItems",
            columns: new[] { "OnHand", "Reserved", "ReorderLevel" });

        migrationBuilder.CreateIndex(
            name: "IX_StockMovements_ProductId_At",
            schema: "inventory",
            table: "StockMovements",
            columns: new[] { "ProductId", "At" });

        migrationBuilder.CreateIndex(
            name: "IX_StockMovements_Reference",
            schema: "inventory",
            table: "StockMovements",
            columns: new[] { "ReferenceType", "ReferenceId" },
            filter: "[ReferenceId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_StockReservations_ProductId_Status",
            schema: "inventory",
            table: "StockReservations",
            columns: new[] { "ProductId", "Status" })
            .Annotation("SqlServer:Include", new[] { "Quantity", "OrderId" });

        migrationBuilder.CreateIndex(
            name: "IX_StockReservations_Status_ExpiresAt",
            schema: "inventory",
            table: "StockReservations",
            columns: new[] { "Status", "ExpiresAt" },
            filter: "[Status] = 'Active' AND [ExpiresAt] IS NOT NULL")
            .Annotation("SqlServer:Include", new[] { "OrderId", "ProductId", "Quantity" });

        migrationBuilder.CreateIndex(
            name: "UQ_StockReservations_OrderId_ProductId",
            schema: "inventory",
            table: "StockReservations",
            columns: new[] { "OrderId", "ProductId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Suppliers_Name",
            schema: "purchasing",
            table: "Suppliers",
            column: "Name",
            unique: true,
            filter: "[DeletedAt] IS NULL");

        migrationBuilder.CreateIndex(
            name: "UQ_Suppliers_Code",
            schema: "purchasing",
            table: "Suppliers",
            column: "Code",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Banners",
            schema: "catalog");

        migrationBuilder.DropTable(
            name: "GoodsReceiptItems",
            schema: "purchasing");

        migrationBuilder.DropTable(
            name: "ProductImages",
            schema: "catalog");

        migrationBuilder.DropTable(
            name: "ProductPriceHistory",
            schema: "catalog");

        migrationBuilder.DropTable(
            name: "PurchaseOrderItems",
            schema: "purchasing");

        migrationBuilder.DropTable(
            name: "StockMovements",
            schema: "inventory");

        migrationBuilder.DropTable(
            name: "StockReservations",
            schema: "inventory");

        migrationBuilder.DropTable(
            name: "GoodsReceipts",
            schema: "purchasing");

        migrationBuilder.DropTable(
            name: "Products",
            schema: "catalog");

        migrationBuilder.DropTable(
            name: "StockItems",
            schema: "inventory");

        migrationBuilder.DropTable(
            name: "PurchaseOrders",
            schema: "purchasing");

        migrationBuilder.DropTable(
            name: "Brands",
            schema: "catalog");

        migrationBuilder.DropTable(
            name: "Categories",
            schema: "catalog");

        migrationBuilder.DropTable(
            name: "Suppliers",
            schema: "purchasing");
    }
}
