using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TailorSpadi.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialIdentityAndTenancy : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AuditLog",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                UserId = table.Column<Guid>(type: "uuid", nullable: true),
                Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Detail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLog", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                NormalizedEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                FirstName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                LastName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                SecurityStamp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                IsPlatformAdmin = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastLoginAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                FailedLoginCount = table.Column<int>(type: "integer", nullable: false),
                LockoutEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "PasswordResetTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_PasswordResetTokens_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Tenants",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                Subdomain = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: true),
                PreferredSlugBase = table.Column<string>(type: "character varying(59)", maxLength: 59, nullable: true),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                WhatsApp = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                City = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                State = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Country = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                LogoStorageKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                PrimaryColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                SecondaryColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                OnboardingStep = table.Column<int>(type: "integer", nullable: false),
                DraftOwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tenants", x => x.Id);
                table.ForeignKey(
                    name: "FK_Tenants_Users_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Tenants_Users_DraftOwnerUserId",
                    column: x => x.DraftOwnerUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                Scope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                SecurityStamp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ReplacedByTokenId = table.Column<Guid>(type: "uuid", nullable: true),
                UserAgent = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_RefreshTokens_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_RefreshTokens_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "TenantMemberships",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TenantMemberships", x => x.Id);
                table.ForeignKey(
                    name: "FK_TenantMemberships_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_TenantMemberships_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLog_CreatedAtUtc",
            table: "AuditLog",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLog_TenantId",
            table: "AuditLog",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLog_UserId",
            table: "AuditLog",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_PasswordResetTokens_TokenHash",
            table: "PasswordResetTokens",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PasswordResetTokens_UserId",
            table: "PasswordResetTokens",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_FamilyId",
            table: "RefreshTokens",
            column: "FamilyId");

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_TenantId",
            table: "RefreshTokens",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_TokenHash",
            table: "RefreshTokens",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_UserId",
            table: "RefreshTokens",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_TenantMemberships_TenantId_UserId",
            table: "TenantMemberships",
            columns: new[] { "TenantId", "UserId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TenantMemberships_UserId",
            table: "TenantMemberships",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Tenants_CreatedByUserId",
            table: "Tenants",
            column: "CreatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_Tenants_DraftOwnerUserId",
            table: "Tenants",
            column: "DraftOwnerUserId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Tenants_Subdomain",
            table: "Tenants",
            column: "Subdomain",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_NormalizedEmail",
            table: "Users",
            column: "NormalizedEmail",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AuditLog");

        migrationBuilder.DropTable(
            name: "PasswordResetTokens");

        migrationBuilder.DropTable(
            name: "RefreshTokens");

        migrationBuilder.DropTable(
            name: "TenantMemberships");

        migrationBuilder.DropTable(
            name: "Tenants");

        migrationBuilder.DropTable(
            name: "Users");
    }
}
