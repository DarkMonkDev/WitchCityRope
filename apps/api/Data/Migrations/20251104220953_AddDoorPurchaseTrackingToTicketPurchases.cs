using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDoorPurchaseTrackingToTicketPurchases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                schema: "public",
                table: "TicketPurchases",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<Guid>(
                name: "RecordedByStaffId",
                schema: "public",
                table: "TicketPurchases",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketPurchases_RecordedByStaffId",
                schema: "public",
                table: "TicketPurchases",
                column: "RecordedByStaffId",
                filter: "\"RecordedByStaffId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_TicketPurchases_Notes_MaxLength",
                schema: "public",
                table: "TicketPurchases",
                sql: "LENGTH(\"Notes\") <= 500 OR \"Notes\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketPurchases_Users_RecordedByStaffId",
                schema: "public",
                table: "TicketPurchases",
                column: "RecordedByStaffId",
                principalSchema: "public",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketPurchases_Users_RecordedByStaffId",
                schema: "public",
                table: "TicketPurchases");

            migrationBuilder.DropIndex(
                name: "IX_TicketPurchases_RecordedByStaffId",
                schema: "public",
                table: "TicketPurchases");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_TicketPurchases_Notes_MaxLength",
                schema: "public",
                table: "TicketPurchases");

            migrationBuilder.DropColumn(
                name: "RecordedByStaffId",
                schema: "public",
                table: "TicketPurchases");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                schema: "public",
                table: "TicketPurchases",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);
        }
    }
}
