using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TailorSpadi.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class EnforceIdentityRelationships : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_TenantMemberships_TenantId_UserId",
            table: "TenantMemberships");

        migrationBuilder.DropIndex(
            name: "IX_RefreshTokens_TenantId",
            table: "RefreshTokens");

        migrationBuilder.AddUniqueConstraint(
            name: "AK_TenantMemberships_TenantId_UserId",
            table: "TenantMemberships",
            columns: new[] { "TenantId", "UserId" });

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_ReplacedByTokenId",
            table: "RefreshTokens",
            column: "ReplacedByTokenId");

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_TenantId_UserId",
            table: "RefreshTokens",
            columns: new[] { "TenantId", "UserId" });

        migrationBuilder.AddCheckConstraint(
            name: "CK_RefreshTokens_Scope",
            table: "RefreshTokens",
            sql: "(\"Scope\" = 'Tenant' AND \"TenantId\" IS NOT NULL) OR (\"Scope\" IN ('Platform', 'Admin') AND \"TenantId\" IS NULL)");

        migrationBuilder.AddForeignKey(
            name: "FK_AuditLog_Tenants_TenantId",
            table: "AuditLog",
            column: "TenantId",
            principalTable: "Tenants",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_AuditLog_Users_UserId",
            table: "AuditLog",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_RefreshTokens_RefreshTokens_ReplacedByTokenId",
            table: "RefreshTokens",
            column: "ReplacedByTokenId",
            principalTable: "RefreshTokens",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_RefreshTokens_TenantMemberships_TenantId_UserId",
            table: "RefreshTokens",
            columns: new[] { "TenantId", "UserId" },
            principalTable: "TenantMemberships",
            principalColumns: new[] { "TenantId", "UserId" },
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_AuditLog_Tenants_TenantId",
            table: "AuditLog");

        migrationBuilder.DropForeignKey(
            name: "FK_AuditLog_Users_UserId",
            table: "AuditLog");

        migrationBuilder.DropForeignKey(
            name: "FK_RefreshTokens_RefreshTokens_ReplacedByTokenId",
            table: "RefreshTokens");

        migrationBuilder.DropForeignKey(
            name: "FK_RefreshTokens_TenantMemberships_TenantId_UserId",
            table: "RefreshTokens");

        migrationBuilder.DropUniqueConstraint(
            name: "AK_TenantMemberships_TenantId_UserId",
            table: "TenantMemberships");

        migrationBuilder.DropIndex(
            name: "IX_RefreshTokens_ReplacedByTokenId",
            table: "RefreshTokens");

        migrationBuilder.DropIndex(
            name: "IX_RefreshTokens_TenantId_UserId",
            table: "RefreshTokens");

        migrationBuilder.DropCheckConstraint(
            name: "CK_RefreshTokens_Scope",
            table: "RefreshTokens");

        migrationBuilder.CreateIndex(
            name: "IX_TenantMemberships_TenantId_UserId",
            table: "TenantMemberships",
            columns: new[] { "TenantId", "UserId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_TenantId",
            table: "RefreshTokens",
            column: "TenantId");
    }
}
