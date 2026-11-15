using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroceryHub.Persistence.Migrations;

/// <inheritdoc />
public partial class CartAndOrdering : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "customers");

        migrationBuilder.EnsureSchema(
            name: "cart");

        migrationBuilder.EnsureSchema(
            name: "promotions");

        migrationBuilder.AddColumn<string>(
            name: "ChecksumSha256",
            schema: "shared",
            table: "AdminUnitDataVersion",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ProvinceCount",
            schema: "shared",
            table: "AdminUnitDataVersion",
            type: "int",
            nullable: false,
            defaultValue: 34);

        migrationBuilder.AddColumn<int>(
            name: "WardCount",
            schema: "shared",
            table: "AdminUnitDataVersion",
            type: "int",
            nullable: false,
            defaultValue: 3321);

        migrationBuilder.CreateTable(
            name: "Addresses",
            schema: "customers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RecipientName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                ProvinceCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                WardCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                ProvinceName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                WardName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Street = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                Note = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Addresses", x => x.Id);
                table.ForeignKey(
                    name: "FK_Addresses_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Carts",
            schema: "cart",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                GuestCartToken = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Carts", x => x.Id);
                table.ForeignKey(
                    name: "FK_Carts_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Coupons",
            schema: "promotions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Type = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                Value = table.Column<long>(type: "bigint", nullable: false),
                MaxDiscount = table.Column<long>(type: "bigint", nullable: true),
                MinSubtotal = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UsageLimit = table.Column<int>(type: "int", nullable: true),
                PerUserLimit = table.Column<int>(type: "int", nullable: true, defaultValue: 1),
                UsedCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "SYSDATETIMEOFFSET()"),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Coupons", x => x.Id);
                table.CheckConstraint("CK_Coupons_Limits", "[MinSubtotal] >= 0 AND ([MaxDiscount] IS NULL OR [MaxDiscount] > 0) AND [UsedCount] >= 0 AND ([UsageLimit] IS NULL OR ([UsageLimit] > 0 AND [UsedCount] <= [UsageLimit])) AND ([PerUserLimit] IS NULL OR [PerUserLimit] > 0) AND [StartsAt] < [EndsAt]");
                table.CheckConstraint("CK_Coupons_Type", "[Type] IN ('Percent', 'FixedAmount', 'FreeShipping')");
                table.CheckConstraint("CK_Coupons_ValueByType", "([Type] = 'Percent' AND [Value] BETWEEN 1 AND 100) OR ([Type] = 'FixedAmount' AND [Value] > 0) OR ([Type] = 'FreeShipping' AND [Value] = 0)");
            });

        migrationBuilder.CreateTable(
            name: "Orders",
            schema: "ordering",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrderNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                Subtotal = table.Column<long>(type: "bigint", nullable: false),
                CouponDiscount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                ShippingFee = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                Total = table.Column<long>(type: "bigint", nullable: false),
                PaymentMethod = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                PaymentStatus = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                ShippingRecipientName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                ShippingPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                ShippingProvinceCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                ShippingWardCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                ShippingProvinceName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                ShippingWardName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                ShippingStreet = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                ShippingNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CancelledBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Orders", x => x.Id);
                table.CheckConstraint("CK_Orders_Amounts", "[Subtotal] >= 0 AND [CouponDiscount] >= 0 AND [ShippingFee] >= 0 AND [Total] >= 0");
                table.CheckConstraint("CK_Orders_PaymentMethod", "[PaymentMethod] IN ('COD', 'SEPAY_QR')");
                table.CheckConstraint("CK_Orders_PaymentStatus", "[PaymentStatus] IN ('Unpaid', 'Pending', 'Paid', 'Failed', 'Expired', 'PartiallyRefunded', 'Refunded')");
                table.CheckConstraint("CK_Orders_Status", "[Status] IN ('PendingPayment', 'Placed', 'Confirmed', 'Preparing', 'Packed', 'Shipping', 'Delivered', 'Cancelled', 'DeliveryFailed')");
                table.ForeignKey(
                    name: "FK_Orders_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Payments",
            schema: "payments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PaymentCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Method = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                Amount = table.Column<long>(type: "bigint", nullable: false),
                PaidAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Payments", x => x.Id);
                table.CheckConstraint("CK_Payments_Amount", "[Amount] >= 0");
                table.CheckConstraint("CK_Payments_Method", "[Method] IN ('COD', 'SEPAY_QR')");
                table.CheckConstraint("CK_Payments_Status", "[Status] IN ('Unpaid', 'Pending', 'Paid', 'Failed', 'Expired', 'PartiallyRefunded', 'Refunded')");
            });

        migrationBuilder.CreateTable(
            name: "ShippingZones",
            schema: "ordering",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ShippingZones", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "CartItems",
            schema: "cart",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CartItems", x => x.Id);
                table.CheckConstraint("CK_CartItems_Quantity", "[Quantity] BETWEEN 1 AND 99");
                table.ForeignKey(
                    name: "FK_CartItems_Carts_CartId",
                    column: x => x.CartId,
                    principalSchema: "cart",
                    principalTable: "Carts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CartItems_Products_ProductId",
                    column: x => x.ProductId,
                    principalSchema: "catalog",
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "CouponRedemptions",
            schema: "promotions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CouponId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Amount = table.Column<long>(type: "bigint", nullable: false),
                RedeemedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                ReleasedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CouponRedemptions", x => x.Id);
                table.CheckConstraint("CK_CouponRedemptions_Amount", "[Amount] >= 0");
                table.ForeignKey(
                    name: "FK_CouponRedemptions_Coupons_CouponId",
                    column: x => x.CouponId,
                    principalSchema: "promotions",
                    principalTable: "Coupons",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CouponRedemptions_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "CouponScopes",
            schema: "promotions",
            columns: table => new
            {
                CouponId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ScopeType = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                ScopeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CouponScopes", x => new { x.CouponId, x.ScopeType, x.ScopeId });
                table.CheckConstraint("CK_CouponScopes_ScopeType", "[ScopeType] IN ('Category', 'Product')");
                table.ForeignKey(
                    name: "FK_CouponScopes_Coupons_CouponId",
                    column: x => x.CouponId,
                    principalSchema: "promotions",
                    principalTable: "Coupons",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "OrderItems",
            schema: "ordering",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                ProductSku = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                ProductUnit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                ProductPackSize = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                UnitPrice = table.Column<long>(type: "bigint", nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false),
                Subtotal = table.Column<long>(type: "bigint", nullable: false),
                DiscountAllocated = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                UnitCostSnapshot = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrderItems", x => x.Id);
                table.CheckConstraint("CK_OrderItems_Amounts", "[UnitPrice] >= 0 AND [Subtotal] >= 0 AND [DiscountAllocated] >= 0 AND [UnitCostSnapshot] >= 0");
                table.CheckConstraint("CK_OrderItems_Quantity", "[Quantity] BETWEEN 1 AND 99");
                table.ForeignKey(
                    name: "FK_OrderItems_Orders_OrderId",
                    column: x => x.OrderId,
                    principalSchema: "ordering",
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_OrderItems_Products_ProductId",
                    column: x => x.ProductId,
                    principalSchema: "catalog",
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "OrderStatusHistory",
            schema: "ordering",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FromStatus = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                ToStatus = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ActorRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrderStatusHistory", x => x.Id);
                table.ForeignKey(
                    name: "FK_OrderStatusHistory_Orders_OrderId",
                    column: x => x.OrderId,
                    principalSchema: "ordering",
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_OrderStatusHistory_Users_ActorUserId",
                    column: x => x.ActorUserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "CodCollections",
            schema: "payments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CollectedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Amount = table.Column<long>(type: "bigint", nullable: false),
                CollectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                Note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CodCollections", x => x.Id);
                table.CheckConstraint("CK_CodCollections_Amount", "[Amount] > 0");
                table.ForeignKey(
                    name: "FK_CodCollections_Payments_PaymentId",
                    column: x => x.PaymentId,
                    principalSchema: "payments",
                    principalTable: "Payments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CodCollections_Users_CollectedBy",
                    column: x => x.CollectedBy,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PaymentAttempts",
            schema: "payments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AttemptNumber = table.Column<int>(type: "int", nullable: false),
                Amount = table.Column<long>(type: "bigint", nullable: false),
                Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentAttempts", x => x.Id);
                table.CheckConstraint("CK_PaymentAttempts_Amount", "[Amount] > 0");
                table.CheckConstraint("CK_PaymentAttempts_Status", "[Status] IN ('Open', 'Paid', 'Expired', 'Superseded')");
                table.ForeignKey(
                    name: "FK_PaymentAttempts_Payments_PaymentId",
                    column: x => x.PaymentId,
                    principalSchema: "payments",
                    principalTable: "Payments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ShippingRates",
            schema: "ordering",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ZoneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BaseFee = table.Column<long>(type: "bigint", nullable: false),
                FreeShippingThreshold = table.Column<long>(type: "bigint", nullable: false, defaultValue: 300000L),
                EstimatedDaysMin = table.Column<int>(type: "int", nullable: false),
                EstimatedDaysMax = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ShippingRates", x => x.Id);
                table.CheckConstraint("CK_ShippingRates_Amounts", "[BaseFee] >= 0 AND [FreeShippingThreshold] >= 0 AND [EstimatedDaysMin] >= 0 AND [EstimatedDaysMax] >= [EstimatedDaysMin]");
                table.ForeignKey(
                    name: "FK_ShippingRates_ShippingZones_ZoneId",
                    column: x => x.ZoneId,
                    principalSchema: "ordering",
                    principalTable: "ShippingZones",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ShippingZoneProvinces",
            schema: "ordering",
            columns: table => new
            {
                ZoneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProvinceCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ShippingZoneProvinces", x => new { x.ZoneId, x.ProvinceCode });
                table.ForeignKey(
                    name: "FK_ShippingZoneProvinces_ShippingZones_ZoneId",
                    column: x => x.ZoneId,
                    principalSchema: "ordering",
                    principalTable: "ShippingZones",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Addresses_UserId_DeletedAt",
            schema: "customers",
            table: "Addresses",
            columns: new[] { "UserId", "DeletedAt" })
            .Annotation("SqlServer:Include", new[] { "IsDefault", "ProvinceCode", "WardCode" });

        migrationBuilder.CreateIndex(
            name: "UQ_Addresses_UserId_Default",
            schema: "customers",
            table: "Addresses",
            column: "UserId",
            unique: true,
            filter: "[IsDefault] = 1 AND [DeletedAt] IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_CartItems_ProductId",
            schema: "cart",
            table: "CartItems",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "UQ_CartItems_CartId_ProductId",
            schema: "cart",
            table: "CartItems",
            columns: new[] { "CartId", "ProductId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Carts_UserId",
            schema: "cart",
            table: "Carts",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "UQ_Carts_GuestCartToken",
            schema: "cart",
            table: "Carts",
            column: "GuestCartToken",
            unique: true,
            filter: "[GuestCartToken] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_CodCollections_CollectedBy",
            schema: "payments",
            table: "CodCollections",
            column: "CollectedBy");

        migrationBuilder.CreateIndex(
            name: "IX_CodCollections_OrderId",
            schema: "payments",
            table: "CodCollections",
            column: "OrderId");

        migrationBuilder.CreateIndex(
            name: "IX_CodCollections_PaymentId",
            schema: "payments",
            table: "CodCollections",
            column: "PaymentId");

        migrationBuilder.CreateIndex(
            name: "IX_CouponRedemptions_CouponId_UserId",
            schema: "promotions",
            table: "CouponRedemptions",
            columns: new[] { "CouponId", "UserId" },
            filter: "[ReleasedAt] IS NULL")
            .Annotation("SqlServer:Include", new[] { "OrderId", "Amount", "RedeemedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_CouponRedemptions_UserId",
            schema: "promotions",
            table: "CouponRedemptions",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "UQ_CouponRedemptions_OrderId",
            schema: "promotions",
            table: "CouponRedemptions",
            column: "OrderId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Coupons_IsActive_Dates",
            schema: "promotions",
            table: "Coupons",
            columns: new[] { "IsActive", "StartsAt", "EndsAt" })
            .Annotation("SqlServer:Include", new[] { "Code", "Type", "Value", "MaxDiscount", "MinSubtotal", "UsageLimit", "UsedCount" });

        migrationBuilder.CreateIndex(
            name: "UQ_Coupons_Code",
            schema: "promotions",
            table: "Coupons",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_OrderItems_ProductId",
            schema: "ordering",
            table: "OrderItems",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "UQ_OrderItems_OrderId_ProductId",
            schema: "ordering",
            table: "OrderItems",
            columns: new[] { "OrderId", "ProductId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Orders_Status_CreatedAt",
            schema: "ordering",
            table: "Orders",
            columns: new[] { "Status", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Orders_UserId_CreatedAt",
            schema: "ordering",
            table: "Orders",
            columns: new[] { "UserId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "UQ_Orders_OrderNumber",
            schema: "ordering",
            table: "Orders",
            column: "OrderNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_OrderStatusHistory_ActorUserId",
            schema: "ordering",
            table: "OrderStatusHistory",
            column: "ActorUserId");

        migrationBuilder.CreateIndex(
            name: "IX_OrderStatusHistory_OrderId_CreatedAt",
            schema: "ordering",
            table: "OrderStatusHistory",
            columns: new[] { "OrderId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_PaymentAttempts_PaymentId_CreatedAt",
            schema: "payments",
            table: "PaymentAttempts",
            columns: new[] { "PaymentId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "UQ_Payments_OrderId",
            schema: "payments",
            table: "Payments",
            column: "OrderId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UQ_Payments_PaymentCode",
            schema: "payments",
            table: "Payments",
            column: "PaymentCode",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ShippingRates_ZoneId",
            schema: "ordering",
            table: "ShippingRates",
            column: "ZoneId");

        migrationBuilder.CreateIndex(
            name: "UQ_ShippingZoneProvinces_ProvinceCode",
            schema: "ordering",
            table: "ShippingZoneProvinces",
            column: "ProvinceCode",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UQ_ShippingZones_Name",
            schema: "ordering",
            table: "ShippingZones",
            column: "Name",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Addresses",
            schema: "customers");

        migrationBuilder.DropTable(
            name: "CartItems",
            schema: "cart");

        migrationBuilder.DropTable(
            name: "CodCollections",
            schema: "payments");

        migrationBuilder.DropTable(
            name: "CouponRedemptions",
            schema: "promotions");

        migrationBuilder.DropTable(
            name: "CouponScopes",
            schema: "promotions");

        migrationBuilder.DropTable(
            name: "OrderItems",
            schema: "ordering");

        migrationBuilder.DropTable(
            name: "OrderStatusHistory",
            schema: "ordering");

        migrationBuilder.DropTable(
            name: "PaymentAttempts",
            schema: "payments");

        migrationBuilder.DropTable(
            name: "ShippingRates",
            schema: "ordering");

        migrationBuilder.DropTable(
            name: "ShippingZoneProvinces",
            schema: "ordering");

        migrationBuilder.DropTable(
            name: "Carts",
            schema: "cart");

        migrationBuilder.DropTable(
            name: "Coupons",
            schema: "promotions");

        migrationBuilder.DropTable(
            name: "Orders",
            schema: "ordering");

        migrationBuilder.DropTable(
            name: "Payments",
            schema: "payments");

        migrationBuilder.DropTable(
            name: "ShippingZones",
            schema: "ordering");

        migrationBuilder.DropColumn(
            name: "ChecksumSha256",
            schema: "shared",
            table: "AdminUnitDataVersion");

        migrationBuilder.DropColumn(
            name: "ProvinceCount",
            schema: "shared",
            table: "AdminUnitDataVersion");

        migrationBuilder.DropColumn(
            name: "WardCount",
            schema: "shared",
            table: "AdminUnitDataVersion");
    }
}
