using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLoggingInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "logging");

            migrationBuilder.CreateTable(
                name: "application_logs",
                schema: "logging",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    timestamp = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    level = table.Column<short>(type: "smallint", nullable: false),
                    level_name = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    message_template = table.Column<string>(type: "text", nullable: true),
                    exception = table.Column<string>(type: "text", nullable: true),
                    source_context = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    properties = table.Column<string>(type: "jsonb", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    request_path = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    machine_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "daily_log_summaries",
                schema: "logging",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Subcategory = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_log_summaries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_application_logs_correlation_id",
                schema: "logging",
                table: "application_logs",
                column: "correlation_id",
                filter: "correlation_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_application_logs_level",
                schema: "logging",
                table: "application_logs",
                column: "level");

            migrationBuilder.CreateIndex(
                name: "IX_application_logs_properties_gin",
                schema: "logging",
                table: "application_logs",
                column: "properties")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_application_logs_recent_errors",
                schema: "logging",
                table: "application_logs",
                columns: new[] { "timestamp", "source_context" },
                filter: "level >= 3");

            migrationBuilder.CreateIndex(
                name: "IX_application_logs_timestamp",
                schema: "logging",
                table: "application_logs",
                column: "timestamp")
                .Annotation("Npgsql:IndexMethod", "brin");

            migrationBuilder.CreateIndex(
                name: "IX_application_logs_user_id",
                schema: "logging",
                table: "application_logs",
                column: "user_id",
                filter: "user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_daily_log_summaries_category",
                schema: "logging",
                table: "daily_log_summaries",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_daily_log_summaries_date_category",
                schema: "logging",
                table: "daily_log_summaries",
                columns: new[] { "Date", "Category" });

            migrationBuilder.CreateIndex(
                name: "UQ_daily_log_summaries_date_category_subcategory",
                schema: "logging",
                table: "daily_log_summaries",
                columns: new[] { "Date", "Category", "Subcategory" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "application_logs",
                schema: "logging");

            migrationBuilder.DropTable(
                name: "daily_log_summaries",
                schema: "logging");
        }
    }
}
