using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledDeliveryStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CHK_SentAdHocEmails_DeliveryStatus",
                table: "SentAdHocEmails");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_SentAdHocEmails_DeliveryStatus",
                table: "SentAdHocEmails",
                sql: "\"DeliveryStatus\" IN ('Pending', 'Scheduled', 'Sent', 'Delivered', 'Failed', 'Bounced')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CHK_SentAdHocEmails_DeliveryStatus",
                table: "SentAdHocEmails");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_SentAdHocEmails_DeliveryStatus",
                table: "SentAdHocEmails",
                sql: "\"DeliveryStatus\" IN ('Pending', 'Sent', 'Delivered', 'Failed', 'Bounced')");
        }
    }
}
