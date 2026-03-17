using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVettingReminderTrackingAndFixEmailLogTemplateType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TemplateType",
                table: "VettingEmailLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReminderSentAt",
                table: "VettingApplications",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemindersSentCount",
                table: "VettingApplications",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReminderSentAt",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "RemindersSentCount",
                table: "VettingApplications");

            migrationBuilder.AlterColumn<int>(
                name: "TemplateType",
                table: "VettingEmailLogs",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
        }
    }
}
