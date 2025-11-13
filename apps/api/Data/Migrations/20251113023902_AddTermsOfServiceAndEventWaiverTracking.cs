using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTermsOfServiceAndEventWaiverTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TermsOfServiceAccepted",
                schema: "public",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TermsOfServiceAcceptedAt",
                schema: "public",
                table: "Users",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EventWaiverAccepted",
                schema: "public",
                table: "TicketPurchases",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "EventWaiverAcceptedAt",
                schema: "public",
                table: "TicketPurchases",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EventWaiverAccepted",
                schema: "public",
                table: "EventAttendances",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "EventWaiverAcceptedAt",
                schema: "public",
                table: "EventAttendances",
                type: "timestamptz",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TermsOfServiceAccepted",
                schema: "public",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TermsOfServiceAcceptedAt",
                schema: "public",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EventWaiverAccepted",
                schema: "public",
                table: "TicketPurchases");

            migrationBuilder.DropColumn(
                name: "EventWaiverAcceptedAt",
                schema: "public",
                table: "TicketPurchases");

            migrationBuilder.DropColumn(
                name: "EventWaiverAccepted",
                schema: "public",
                table: "EventAttendances");

            migrationBuilder.DropColumn(
                name: "EventWaiverAcceptedAt",
                schema: "public",
                table: "EventAttendances");
        }
    }
}
