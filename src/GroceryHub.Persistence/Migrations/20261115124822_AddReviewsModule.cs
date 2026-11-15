using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroceryHub.Persistence.Migrations;

/// <inheritdoc />
public partial class AddReviewsModule : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "reviews");

        migrationBuilder.CreateTable(
            name: "Reviews",
            schema: "reviews",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Rating = table.Column<int>(type: "int", nullable: false),
                Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                ModeratedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ModeratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                ReplyBody = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                ReplyBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ReplyAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                EditedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Reviews", x => x.Id);
                table.CheckConstraint("CK_Reviews_Rating", "[Rating] BETWEEN 1 AND 5");
                table.CheckConstraint("CK_Reviews_Status", "[Status] IN ('Pending', 'Approved', 'Rejected')");
                table.ForeignKey(
                    name: "FK_Reviews_OrderItems_OrderItemId",
                    column: x => x.OrderItemId,
                    principalSchema: "ordering",
                    principalTable: "OrderItems",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_Reviews_Products_ProductId",
                    column: x => x.ProductId,
                    principalSchema: "catalog",
                    principalTable: "Products",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_Reviews_Users_ModeratedBy",
                    column: x => x.ModeratedBy,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_Reviews_Users_ReplyBy",
                    column: x => x.ReplyBy,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_Reviews_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_Reviews_ModeratedBy",
            schema: "reviews",
            table: "Reviews",
            column: "ModeratedBy");

        migrationBuilder.CreateIndex(
            name: "IX_Reviews_OrderItemId",
            schema: "reviews",
            table: "Reviews",
            column: "OrderItemId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Reviews_ProductId_Status_CreatedAt",
            schema: "reviews",
            table: "Reviews",
            columns: new[] { "ProductId", "Status", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Reviews_ReplyBy",
            schema: "reviews",
            table: "Reviews",
            column: "ReplyBy");

        migrationBuilder.CreateIndex(
            name: "IX_Reviews_UserId",
            schema: "reviews",
            table: "Reviews",
            column: "UserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Reviews",
            schema: "reviews");
    }
}
