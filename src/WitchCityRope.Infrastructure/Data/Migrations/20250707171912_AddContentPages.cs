using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WitchCityRope.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContentPages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cms");

            migrationBuilder.AlterColumn<string>(
                name: "EmergencyContactPhone",
                table: "Registrations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "EmergencyContactName",
                table: "Registrations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "DietaryRestrictions",
                table: "Registrations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "CancellationReason",
                table: "Registrations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "AccessibilityNeeds",
                table: "Registrations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<Guid>(
                name: "InstructorId",
                table: "Events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresVetting",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SkillLevel",
                table: "Events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Events",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ContentPages",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    HasBeenEdited = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    MetaDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MetaKeywords = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentPages", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "cms",
                table: "ContentPages",
                columns: new[] { "Id", "Content", "CreatedAt", "LastModifiedBy", "MetaDescription", "MetaKeywords", "Slug", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Default Text Place Holder", new DateTime(2025, 7, 7, 17, 19, 12, 306, DateTimeKind.Utc).AddTicks(6623), "system", null, null, "resources", "Resources", new DateTime(2025, 7, 7, 17, 19, 12, 306, DateTimeKind.Utc).AddTicks(6623) },
                    { 2, "Default Text Place Holder", new DateTime(2025, 7, 7, 17, 19, 12, 306, DateTimeKind.Utc).AddTicks(6623), "system", null, null, "private-lessons", "Private Lessons", new DateTime(2025, 7, 7, 17, 19, 12, 306, DateTimeKind.Utc).AddTicks(6623) },
                    { 3, "Default Text Place Holder", new DateTime(2025, 7, 7, 17, 19, 12, 306, DateTimeKind.Utc).AddTicks(6623), "system", null, null, "contact", "Contact Us", new DateTime(2025, 7, 7, 17, 19, 12, 306, DateTimeKind.Utc).AddTicks(6623) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentPages_LastModifiedBy",
                schema: "cms",
                table: "ContentPages",
                column: "LastModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPages_Slug",
                schema: "cms",
                table: "ContentPages",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentPages",
                schema: "cms");

            migrationBuilder.DropColumn(
                name: "InstructorId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RequiresVetting",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "SkillLevel",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Events");

            migrationBuilder.AlterColumn<string>(
                name: "EmergencyContactPhone",
                table: "Registrations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EmergencyContactName",
                table: "Registrations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DietaryRestrictions",
                table: "Registrations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CancellationReason",
                table: "Registrations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AccessibilityNeeds",
                table: "Registrations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
