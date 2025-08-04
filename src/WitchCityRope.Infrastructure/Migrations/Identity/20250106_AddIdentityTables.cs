using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace WitchCityRope.Infrastructure.Migrations.Identity
{
    /// <summary>
    /// Migration to add ASP.NET Core Identity tables while preserving existing user data
    /// </summary>
    public partial class AddIdentityTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create auth schema if it doesn't exist
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS auth;");

            // Create Identity tables in auth schema
            // AspNetUsers table (will be mapped from existing Users table)
            migrationBuilder.CreateTable(
                name: "Users",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false),
                    // Custom fields from WitchCityRopeUser
                    EncryptedLegalName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SceneName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EmailValue = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    EmailDisplayValue = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsVetted = table.Column<bool>(type: "boolean", nullable: false),
                    PronouncedName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Pronouns = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "integer", nullable: false),
                    LockedOutUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastPasswordChangeAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmailVerificationToken = table.Column<string>(type: "text", nullable: true),
                    EmailVerificationTokenCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            // AspNetRoles table
            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    // Custom fields from WitchCityRopeRole
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            // AspNetUserRoles table
            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "auth",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "auth",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // AspNetUserClaims table
            migrationBuilder.CreateTable(
                name: "UserClaims",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // AspNetUserLogins table
            migrationBuilder.CreateTable(
                name: "UserLogins",
                schema: "auth",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // AspNetRoleClaims table
            migrationBuilder.CreateTable(
                name: "RoleClaims",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "auth",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // AspNetUserTokens table
            migrationBuilder.CreateTable(
                name: "UserTokens",
                schema: "auth",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "auth",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "auth",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "auth",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_SceneName",
                schema: "auth",
                table: "Users",
                column: "SceneName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                schema: "auth",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsVetted",
                schema: "auth",
                table: "Users",
                column: "IsVetted");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                schema: "auth",
                table: "Users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "auth",
                table: "Roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_IsActive",
                schema: "auth",
                table: "Roles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Priority",
                schema: "auth",
                table: "Roles",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                schema: "auth",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                schema: "auth",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                schema: "auth",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                schema: "auth",
                table: "RoleClaims",
                column: "RoleId");

            // Insert default roles
            migrationBuilder.Sql(@"
                INSERT INTO auth.""Roles"" (""Id"", ""Name"", ""NormalizedName"", ""ConcurrencyStamp"", ""Description"", ""IsActive"", ""Priority"", ""CreatedAt"", ""UpdatedAt"")
                VALUES 
                    ('11111111-1111-1111-1111-111111111111', 'Attendee', 'ATTENDEE', CONCAT(EXTRACT(EPOCH FROM NOW())::BIGINT, '-', gen_random_uuid()), 'Standard event attendee', true, 0, NOW(), NOW()),
                    ('22222222-2222-2222-2222-222222222222', 'Member', 'MEMBER', CONCAT(EXTRACT(EPOCH FROM NOW())::BIGINT, '-', gen_random_uuid()), 'Verified community member with additional privileges', true, 1, NOW(), NOW()),
                    ('33333333-3333-3333-3333-333333333333', 'Organizer', 'ORGANIZER', CONCAT(EXTRACT(EPOCH FROM NOW())::BIGINT, '-', gen_random_uuid()), 'Event organizer who can create and manage events', true, 2, NOW(), NOW()),
                    ('44444444-4444-4444-4444-444444444444', 'Moderator', 'MODERATOR', CONCAT(EXTRACT(EPOCH FROM NOW())::BIGINT, '-', gen_random_uuid()), 'Community moderator who can review incidents and vetting', true, 3, NOW(), NOW()),
                    ('55555555-5555-5555-5555-555555555555', 'Administrator', 'ADMINISTRATOR', CONCAT(EXTRACT(EPOCH FROM NOW())::BIGINT, '-', gen_random_uuid()), 'System administrator with full access', true, 4, NOW(), NOW())
                ON CONFLICT (""Id"") DO NOTHING;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes
            migrationBuilder.DropIndex("EmailIndex", "Users", "auth");
            migrationBuilder.DropIndex("UserNameIndex", "Users", "auth");
            migrationBuilder.DropIndex("IX_Users_Email", "Users", "auth");
            migrationBuilder.DropIndex("IX_Users_SceneName", "Users", "auth");
            migrationBuilder.DropIndex("IX_Users_IsActive", "Users", "auth");
            migrationBuilder.DropIndex("IX_Users_IsVetted", "Users", "auth");
            migrationBuilder.DropIndex("IX_Users_Role", "Users", "auth");
            migrationBuilder.DropIndex("RoleNameIndex", "Roles", "auth");
            migrationBuilder.DropIndex("IX_Roles_IsActive", "Roles", "auth");
            migrationBuilder.DropIndex("IX_Roles_Priority", "Roles", "auth");

            // Drop tables
            migrationBuilder.DropTable("UserTokens", "auth");
            migrationBuilder.DropTable("RoleClaims", "auth");
            migrationBuilder.DropTable("UserLogins", "auth");
            migrationBuilder.DropTable("UserClaims", "auth");
            migrationBuilder.DropTable("UserRoles", "auth");
            migrationBuilder.DropTable("Roles", "auth");
            migrationBuilder.DropTable("Users", "auth");
        }
    }
}