using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class CreatePaymentSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Dictionary<string, object>>(
                name: "Metadata",
                schema: "public",
                table: "Payments",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'",
                oldClrType: typeof(Dictionary<string, object>),
                oldType: "jsonb",
                oldDefaultValue: new Dictionary<string, object>());

            migrationBuilder.AlterColumn<Dictionary<string, object>>(
                name: "Metadata",
                schema: "public",
                table: "PaymentRefunds",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'",
                oldClrType: typeof(Dictionary<string, object>),
                oldType: "jsonb",
                oldDefaultValue: new Dictionary<string, object>());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Dictionary<string, object>>(
                name: "Metadata",
                schema: "public",
                table: "Payments",
                type: "jsonb",
                nullable: false,
                defaultValue: new Dictionary<string, object>(),
                oldClrType: typeof(Dictionary<string, object>),
                oldType: "jsonb",
                oldDefaultValueSql: "'{}'");

            migrationBuilder.AlterColumn<Dictionary<string, object>>(
                name: "Metadata",
                schema: "public",
                table: "PaymentRefunds",
                type: "jsonb",
                nullable: false,
                defaultValue: new Dictionary<string, object>(),
                oldClrType: typeof(Dictionary<string, object>),
                oldType: "jsonb",
                oldDefaultValueSql: "'{}'");
        }
    }
}
