using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIncidentReportingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CoordinatorId",
                schema: "public",
                table: "SafetyIncidents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleDriveFinalReportUrl",
                schema: "public",
                table: "SafetyIncidents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleDriveFolderUrl",
                schema: "public",
                table: "SafetyIncidents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IncidentNotes",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsPrivate = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Tags = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentNotes", x => x.Id);
                    table.CheckConstraint("CHK_IncidentNotes_Content_NotEmpty", "LENGTH(TRIM(\"Content\")) > 0");
                    table.CheckConstraint("CHK_IncidentNotes_Type", "\"Type\" IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_IncidentNotes_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalSchema: "public",
                        principalTable: "SafetyIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IncidentNotes_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidents_CoordinatorId_Status",
                schema: "public",
                table: "SafetyIncidents",
                columns: new[] { "CoordinatorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidents_Status_CoordinatorId",
                schema: "public",
                table: "SafetyIncidents",
                columns: new[] { "Status", "CoordinatorId" },
                filter: "\"CoordinatorId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentNotes_AuthorId",
                schema: "public",
                table: "IncidentNotes",
                column: "AuthorId",
                filter: "\"AuthorId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentNotes_IncidentId_CreatedAt",
                schema: "public",
                table: "IncidentNotes",
                columns: new[] { "IncidentId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentNotes_Type",
                schema: "public",
                table: "IncidentNotes",
                column: "Type");

            migrationBuilder.AddForeignKey(
                name: "FK_SafetyIncidents_Users_CoordinatorId",
                schema: "public",
                table: "SafetyIncidents",
                column: "CoordinatorId",
                principalSchema: "public",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SafetyIncidents_Users_CoordinatorId",
                schema: "public",
                table: "SafetyIncidents");

            migrationBuilder.DropTable(
                name: "IncidentNotes",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_SafetyIncidents_CoordinatorId_Status",
                schema: "public",
                table: "SafetyIncidents");

            migrationBuilder.DropIndex(
                name: "IX_SafetyIncidents_Status_CoordinatorId",
                schema: "public",
                table: "SafetyIncidents");

            migrationBuilder.DropColumn(
                name: "CoordinatorId",
                schema: "public",
                table: "SafetyIncidents");

            migrationBuilder.DropColumn(
                name: "GoogleDriveFinalReportUrl",
                schema: "public",
                table: "SafetyIncidents");

            migrationBuilder.DropColumn(
                name: "GoogleDriveFolderUrl",
                schema: "public",
                table: "SafetyIncidents");
        }
    }
}
