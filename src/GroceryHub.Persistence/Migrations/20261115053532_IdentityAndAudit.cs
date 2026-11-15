using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroceryHub.Persistence.Migrations;

/// <inheritdoc />
public partial class IdentityAndAudit : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "audit");

        migrationBuilder.EnsureSchema(
            name: "identity");

        migrationBuilder.CreateTable(
            name: "AuditLogs",
            schema: "audit",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ActorRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Ip = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogs", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Permissions",
            schema: "identity",
            columns: table => new
            {
                Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Group = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                AllowPerUserGrant = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Permissions", x => x.Code);
            });

        migrationBuilder.CreateTable(
            name: "Roles",
            schema: "identity",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                NormalizedName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                IsSystem = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                ConcurrencyStamp = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Roles", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            schema: "identity",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                EmailConfirmed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "Active"),
                LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                LockoutEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                AccessFailedCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                ForcePasswordChange = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "SYSDATETIMEOFFSET()"),
                DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
                table.CheckConstraint("CK_Users_Status", "[Status] IN ('Active', 'Locked', 'Disabled')");
            });

        migrationBuilder.CreateTable(
            name: "RolePermissions",
            schema: "identity",
            columns: table => new
            {
                RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PermissionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionCode });
                table.ForeignKey(
                    name: "FK_RolePermissions_Permissions_PermissionCode",
                    column: x => x.PermissionCode,
                    principalSchema: "identity",
                    principalTable: "Permissions",
                    principalColumn: "Code",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_RolePermissions_Roles_RoleId",
                    column: x => x.RoleId,
                    principalSchema: "identity",
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            schema: "identity",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                FamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                ReplacedByTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                CreatedByIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_RefreshTokens_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "StaffProfiles",
            schema: "identity",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EmployeeCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Position = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                HiredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StaffProfiles", x => x.UserId);
                table.ForeignKey(
                    name: "FK_StaffProfiles_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserPermissions",
            schema: "identity",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PermissionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                GrantedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                GrantedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserPermissions", x => new { x.UserId, x.PermissionCode });
                table.ForeignKey(
                    name: "FK_UserPermissions_Permissions_PermissionCode",
                    column: x => x.PermissionCode,
                    principalSchema: "identity",
                    principalTable: "Permissions",
                    principalColumn: "Code",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UserPermissions_Users_GrantedBy",
                    column: x => x.GrantedBy,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_UserPermissions_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserRoles",
            schema: "identity",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                table.ForeignKey(
                    name: "FK_UserRoles_Roles_RoleId",
                    column: x => x.RoleId,
                    principalSchema: "identity",
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UserRoles_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "identity",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_ActorUserId_At",
            schema: "audit",
            table: "AuditLogs",
            columns: new[] { "ActorUserId", "At" });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_EntityType_EntityId",
            schema: "audit",
            table: "AuditLogs",
            columns: new[] { "EntityType", "EntityId" });

        migrationBuilder.CreateIndex(
            name: "IX_Permissions_Group_Code",
            schema: "identity",
            table: "Permissions",
            columns: new[] { "Group", "Code" });

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_FamilyId",
            schema: "identity",
            table: "RefreshTokens",
            column: "FamilyId");

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_TokenHash",
            schema: "identity",
            table: "RefreshTokens",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_UserId",
            schema: "identity",
            table: "RefreshTokens",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_UserId_ExpiresAt",
            schema: "identity",
            table: "RefreshTokens",
            columns: new[] { "UserId", "ExpiresAt" });

        migrationBuilder.CreateIndex(
            name: "IX_RolePermissions_PermissionCode",
            schema: "identity",
            table: "RolePermissions",
            column: "PermissionCode");

        migrationBuilder.CreateIndex(
            name: "IX_Roles_Name",
            schema: "identity",
            table: "Roles",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Roles_NormalizedName",
            schema: "identity",
            table: "Roles",
            column: "NormalizedName",
            unique: true,
            filter: "[NormalizedName] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_StaffProfiles_EmployeeCode",
            schema: "identity",
            table: "StaffProfiles",
            column: "EmployeeCode",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UserPermissions_GrantedBy",
            schema: "identity",
            table: "UserPermissions",
            column: "GrantedBy");

        migrationBuilder.CreateIndex(
            name: "IX_UserPermissions_PermissionCode",
            schema: "identity",
            table: "UserPermissions",
            column: "PermissionCode");

        migrationBuilder.CreateIndex(
            name: "IX_UserRoles_RoleId",
            schema: "identity",
            table: "UserRoles",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "IX_Users_NormalizedEmail",
            schema: "identity",
            table: "Users",
            column: "NormalizedEmail",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_NormalizedUserName",
            schema: "identity",
            table: "Users",
            column: "NormalizedUserName",
            unique: true,
            filter: "[NormalizedUserName] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Users_PhoneNumber",
            schema: "identity",
            table: "Users",
            column: "PhoneNumber",
            filter: "[PhoneNumber] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Users_Status_CreatedAt",
            schema: "identity",
            table: "Users",
            columns: new[] { "Status", "CreatedAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AuditLogs",
            schema: "audit");

        migrationBuilder.DropTable(
            name: "RefreshTokens",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "RolePermissions",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "StaffProfiles",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "UserPermissions",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "UserRoles",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "Permissions",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "Roles",
            schema: "identity");

        migrationBuilder.DropTable(
            name: "Users",
            schema: "identity");
    }
}
