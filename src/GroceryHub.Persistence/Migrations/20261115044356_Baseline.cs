using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroceryHub.Persistence.Migrations;

/// <inheritdoc />
public partial class Baseline : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DECLARE @db sysname = DB_NAME(); EXEC(N'ALTER DATABASE [' + @db + N'] SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE');",
            suppressTransaction: true);

        migrationBuilder.EnsureSchema(
            name: DatabaseSchemas.Identity);

        migrationBuilder.EnsureSchema(
            name: DatabaseSchemas.Customers);

        migrationBuilder.EnsureSchema(
            name: DatabaseSchemas.Catalog);

        migrationBuilder.EnsureSchema(
            name: DatabaseSchemas.Inventory);

        migrationBuilder.EnsureSchema(
            name: DatabaseSchemas.Purchasing);

        migrationBuilder.EnsureSchema(
            name: DatabaseSchemas.Promotions);

        migrationBuilder.EnsureSchema(
            name: DatabaseSchemas.Cart);

        migrationBuilder.EnsureSchema(
            name: DatabaseSchemas.Ordering);

        migrationBuilder.EnsureSchema(
            name: DatabaseSchemas.Payments);

        migrationBuilder.EnsureSchema(
            name: DatabaseSchemas.Reviews);

        migrationBuilder.EnsureSchema(
            name: DatabaseSchemas.Audit);

        migrationBuilder.EnsureSchema(
            name: DatabaseSchemas.Shared);

        migrationBuilder.CreateSequence(
            name: "GoodsReceiptSeq",
            schema: "purchasing");

        migrationBuilder.CreateSequence(
            name: "OrderNumberSeq",
            schema: "ordering");

        migrationBuilder.CreateSequence(
            name: "PaymentCodeSeq",
            schema: "payments",
            startValue: 1000000001L);

        migrationBuilder.CreateSequence(
            name: "PurchaseOrderSeq",
            schema: "purchasing");

        migrationBuilder.CreateTable(
            name: "AdminUnitDataVersion",
            schema: "shared",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Version = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                ImportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                Licence = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminUnitDataVersion", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "IdempotencyKeys",
            schema: "ordering",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Scope = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                ResponseStatus = table.Column<int>(type: "int", nullable: false),
                ResponseContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                ResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IdempotencyKeys", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "OutboxConsumerReceipts",
            schema: "shared",
            columns: table => new
            {
                MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                HandlerName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OutboxConsumerReceipts", x => new { x.MessageId, x.HandlerName });
            });

        migrationBuilder.CreateTable(
            name: "OutboxMessages",
            schema: "shared",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EventType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                LockedUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                Attempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OutboxMessages", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Provinces",
            schema: "shared",
            columns: table => new
            {
                Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                FullName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Provinces", x => x.Code);
            });

        migrationBuilder.CreateTable(
            name: "Settings",
            schema: "shared",
            columns: table => new
            {
                Key = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Value = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Settings", x => x.Key);
            });

        migrationBuilder.CreateTable(
            name: "Wards",
            schema: "shared",
            columns: table => new
            {
                Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                ProvinceCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                FullName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Wards", x => x.Code);
                table.ForeignKey(
                    name: "FK_Wards_Provinces_ProvinceCode",
                    column: x => x.ProvinceCode,
                    principalSchema: "shared",
                    principalTable: "Provinces",
                    principalColumn: "Code",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_IdempotencyKeys_ExpiresAt",
            schema: "ordering",
            table: "IdempotencyKeys",
            column: "ExpiresAt");

        migrationBuilder.CreateIndex(
            name: "UQ_IdempotencyKeys_Key_Scope",
            schema: "ordering",
            table: "IdempotencyKeys",
            columns: new[] { "Key", "Scope" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_Unprocessed",
            schema: "shared",
            table: "OutboxMessages",
            column: "OccurredAt",
            filter: "[ProcessedAt] IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Wards_ProvinceCode",
            schema: "shared",
            table: "Wards",
            column: "ProvinceCode");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AdminUnitDataVersion",
            schema: "shared");

        migrationBuilder.DropTable(
            name: "IdempotencyKeys",
            schema: "ordering");

        migrationBuilder.DropTable(
            name: "OutboxConsumerReceipts",
            schema: "shared");

        migrationBuilder.DropTable(
            name: "OutboxMessages",
            schema: "shared");

        migrationBuilder.DropTable(
            name: "Settings",
            schema: "shared");

        migrationBuilder.DropTable(
            name: "Wards",
            schema: "shared");

        migrationBuilder.DropTable(
            name: "Provinces",
            schema: "shared");

        migrationBuilder.DropSequence(
            name: "GoodsReceiptSeq",
            schema: "purchasing");

        migrationBuilder.DropSequence(
            name: "OrderNumberSeq",
            schema: "ordering");

        migrationBuilder.DropSequence(
            name: "PaymentCodeSeq",
            schema: "payments");

        migrationBuilder.DropSequence(
            name: "PurchaseOrderSeq",
            schema: "purchasing");
    }
}
