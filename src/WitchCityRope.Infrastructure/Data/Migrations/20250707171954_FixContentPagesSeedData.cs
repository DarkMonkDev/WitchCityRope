using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixContentPagesSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "cms",
                table: "ContentPages",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 7, 7, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 7, 7, 12, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                schema: "cms",
                table: "ContentPages",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 7, 7, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 7, 7, 12, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                schema: "cms",
                table: "ContentPages",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 7, 7, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 7, 7, 12, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "cms",
                table: "ContentPages",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 7, 7, 17, 19, 12, 306, DateTimeKind.Utc).AddTicks(6623), new DateTime(2025, 7, 7, 17, 19, 12, 306, DateTimeKind.Utc).AddTicks(6623) });

            migrationBuilder.UpdateData(
                schema: "cms",
                table: "ContentPages",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 7, 7, 17, 19, 12, 306, DateTimeKind.Utc).AddTicks(6623), new DateTime(2025, 7, 7, 17, 19, 12, 306, DateTimeKind.Utc).AddTicks(6623) });

            migrationBuilder.UpdateData(
                schema: "cms",
                table: "ContentPages",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 7, 7, 17, 19, 12, 306, DateTimeKind.Utc).AddTicks(6623), new DateTime(2025, 7, 7, 17, 19, 12, 306, DateTimeKind.Utc).AddTicks(6623) });
        }
    }
}
