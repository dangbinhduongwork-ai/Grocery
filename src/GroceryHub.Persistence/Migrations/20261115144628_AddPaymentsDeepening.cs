using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroceryHub.Persistence.Migrations;

/// <inheritdoc />
public partial class AddPaymentsDeepening : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "ExpiresAt",
            schema: "payments",
            table: "PaymentAttempts",
            type: "datetimeoffset",
            nullable: false,
            defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

        migrationBuilder.AddColumn<string>(
            name: "PaymentCode",
            schema: "payments",
            table: "PaymentAttempts",
            type: "nvarchar(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateTable(
            name: "Refunds",
            schema: "payments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Amount = table.Column<long>(type: "bigint", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                RequestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                DecidedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                DecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                CompletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                BankReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Refunds", x => x.Id);
                table.CheckConstraint("CK_Refunds_Amount", "[Amount] > 0");
                table.CheckConstraint("CK_Refunds_Status", "[Status] IN ('Requested', 'Approved', 'Rejected', 'Completed')");
                table.CheckConstraint("CK_Refunds_TwoPersonRule", "[DecidedBy] IS NULL OR [DecidedBy] <> [RequestedBy]");
                table.ForeignKey(
                    name: "FK_Refunds_Orders_OrderId",
                    column: x => x.OrderId,
                    principalSchema: "ordering",
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Refunds_Payments_PaymentId",
                    column: x => x.PaymentId,
                    principalSchema: "payments",
                    principalTable: "Payments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Refunds_Users_CompletedBy",
                    column: x => x.CompletedBy,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Refunds_Users_DecidedBy",
                    column: x => x.DecidedBy,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Refunds_Users_RequestedBy",
                    column: x => x.RequestedBy,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "WebhookEvents",
            schema: "payments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Provider = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                ProviderTransactionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                RawBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                SignatureValid = table.Column<bool>(type: "bit", nullable: false),
                ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                ProcessingStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Error = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                DeliveryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WebhookEvents", x => x.Id);
                table.CheckConstraint("CK_WebhookEvents_ProcessingStatus", "[ProcessingStatus] IN ('Received', 'Processed', 'Ignored', 'Failed')");
            });

        migrationBuilder.CreateTable(
            name: "PaymentTransactions",
            schema: "payments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                WebhookEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PaymentAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Direction = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                Amount = table.Column<long>(type: "bigint", nullable: false),
                AccountNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Gateway = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                ReferenceCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                Content = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                TransactionTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                MatchStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                ReviewStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                ResolvedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                ResolutionNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                table.CheckConstraint("CK_PaymentTransactions_Amount", "[Amount] >= 0");
                table.CheckConstraint("CK_PaymentTransactions_Direction", "[Direction] IN ('in', 'out')");
                table.CheckConstraint("CK_PaymentTransactions_ReviewStatus", "[ReviewStatus] IN ('None', 'NeedsReview', 'Resolved')");
                table.ForeignKey(
                    name: "FK_PaymentTransactions_PaymentAttempts_PaymentAttemptId",
                    column: x => x.PaymentAttemptId,
                    principalSchema: "payments",
                    principalTable: "PaymentAttempts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_PaymentTransactions_Users_ResolvedBy",
                    column: x => x.ResolvedBy,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PaymentTransactions_WebhookEvents_WebhookEventId",
                    column: x => x.WebhookEventId,
                    principalSchema: "payments",
                    principalTable: "WebhookEvents",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PaymentAttempts_ExpiresAt_Open",
            schema: "payments",
            table: "PaymentAttempts",
            column: "ExpiresAt",
            filter: "[Status] = 'Open'");

        migrationBuilder.CreateIndex(
            name: "UQ_PaymentAttempts_PaymentCode",
            schema: "payments",
            table: "PaymentAttempts",
            column: "PaymentCode",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PaymentTransactions_PaymentAttemptId",
            schema: "payments",
            table: "PaymentTransactions",
            column: "PaymentAttemptId");

        migrationBuilder.CreateIndex(
            name: "IX_PaymentTransactions_ReferenceCode",
            schema: "payments",
            table: "PaymentTransactions",
            column: "ReferenceCode");

        migrationBuilder.CreateIndex(
            name: "IX_PaymentTransactions_ResolvedBy",
            schema: "payments",
            table: "PaymentTransactions",
            column: "ResolvedBy");

        migrationBuilder.CreateIndex(
            name: "IX_PaymentTransactions_ReviewStatus",
            schema: "payments",
            table: "PaymentTransactions",
            column: "ReviewStatus");

        migrationBuilder.CreateIndex(
            name: "UQ_PaymentTransactions_WebhookEventId",
            schema: "payments",
            table: "PaymentTransactions",
            column: "WebhookEventId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Refunds_CompletedBy",
            schema: "payments",
            table: "Refunds",
            column: "CompletedBy");

        migrationBuilder.CreateIndex(
            name: "IX_Refunds_DecidedBy",
            schema: "payments",
            table: "Refunds",
            column: "DecidedBy");

        migrationBuilder.CreateIndex(
            name: "IX_Refunds_OrderId",
            schema: "payments",
            table: "Refunds",
            column: "OrderId");

        migrationBuilder.CreateIndex(
            name: "IX_Refunds_PaymentId",
            schema: "payments",
            table: "Refunds",
            column: "PaymentId");

        migrationBuilder.CreateIndex(
            name: "IX_Refunds_RequestedBy",
            schema: "payments",
            table: "Refunds",
            column: "RequestedBy");

        migrationBuilder.CreateIndex(
            name: "IX_Refunds_Status",
            schema: "payments",
            table: "Refunds",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_WebhookEvents_ProcessingStatus_ReceivedAt",
            schema: "payments",
            table: "WebhookEvents",
            columns: new[] { "ProcessingStatus", "ReceivedAt" });

        migrationBuilder.CreateIndex(
            name: "UQ_WebhookEvents_Provider_ProviderTransactionId",
            schema: "payments",
            table: "WebhookEvents",
            columns: new[] { "Provider", "ProviderTransactionId" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PaymentTransactions",
            schema: "payments");

        migrationBuilder.DropTable(
            name: "Refunds",
            schema: "payments");

        migrationBuilder.DropTable(
            name: "WebhookEvents",
            schema: "payments");

        migrationBuilder.DropIndex(
            name: "IX_PaymentAttempts_ExpiresAt_Open",
            schema: "payments",
            table: "PaymentAttempts");

        migrationBuilder.DropIndex(
            name: "UQ_PaymentAttempts_PaymentCode",
            schema: "payments",
            table: "PaymentAttempts");

        migrationBuilder.DropColumn(
            name: "ExpiresAt",
            schema: "payments",
            table: "PaymentAttempts");

        migrationBuilder.DropColumn(
            name: "PaymentCode",
            schema: "payments",
            table: "PaymentAttempts");
    }
}
