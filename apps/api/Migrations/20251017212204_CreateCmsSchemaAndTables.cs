using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class CreateCmsSchemaAndTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cms");

            migrationBuilder.CreateTable(
                name: "ContentPages",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentPages", x => x.Id);
                    table.CheckConstraint("CHK_ContentPages_Content_NotEmpty", "LENGTH(TRIM(\"Content\")) > 0");
                    table.CheckConstraint("CHK_ContentPages_Slug_Format", "\"Slug\" ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
                    table.CheckConstraint("CHK_ContentPages_Title_Length", "LENGTH(TRIM(\"Title\")) >= 3");
                    table.ForeignKey(
                        name: "FK_ContentPages_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContentPages_LastModifiedBy",
                        column: x => x.LastModifiedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContentRevisions",
                schema: "cms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContentPageId = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentRevisions", x => x.Id);
                    table.CheckConstraint("CHK_ContentRevisions_Content_NotEmpty", "LENGTH(TRIM(\"Content\")) > 0");
                    table.CheckConstraint("CHK_ContentRevisions_Title_Length", "LENGTH(TRIM(\"Title\")) >= 3");
                    table.ForeignKey(
                        name: "FK_ContentRevisions_ContentPage",
                        column: x => x.ContentPageId,
                        principalSchema: "cms",
                        principalTable: "ContentPages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentRevisions_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentPages_CreatedBy",
                schema: "cms",
                table: "ContentPages",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPages_IsPublished",
                schema: "cms",
                table: "ContentPages",
                column: "IsPublished",
                filter: "\"IsPublished\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPages_LastModifiedBy",
                schema: "cms",
                table: "ContentPages",
                column: "LastModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPages_UpdatedAt",
                schema: "cms",
                table: "ContentPages",
                column: "UpdatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "UX_ContentPages_Slug",
                schema: "cms",
                table: "ContentPages",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentRevisions_ContentPageId",
                schema: "cms",
                table: "ContentRevisions",
                column: "ContentPageId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentRevisions_ContentPageId_CreatedAt",
                schema: "cms",
                table: "ContentRevisions",
                columns: new[] { "ContentPageId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ContentRevisions_CreatedAt",
                schema: "cms",
                table: "ContentRevisions",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_ContentRevisions_CreatedBy",
                schema: "cms",
                table: "ContentRevisions",
                column: "CreatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentRevisions",
                schema: "cms");

            migrationBuilder.DropTable(
                name: "ContentPages",
                schema: "cms");
        }
    }
}
