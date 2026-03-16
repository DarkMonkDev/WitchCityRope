using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSegmentAndDropDeliveryStatusConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SentAdHocEmails_DeliveryStatus",
                schema: "public",
                table: "SentAdHocEmails");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_SentAdHocEmails_DeliveryStatus",
                schema: "public",
                table: "SentAdHocEmails");

            migrationBuilder.AddColumn<int>(
                name: "Segment",
                schema: "public",
                table: "SentAdHocEmails",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SentAdHocEmails_DeliveryStatus",
                schema: "public",
                table: "SentAdHocEmails",
                column: "DeliveryStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SentAdHocEmails_DeliveryStatus",
                schema: "public",
                table: "SentAdHocEmails");

            migrationBuilder.DropColumn(
                name: "Segment",
                schema: "public",
                table: "SentAdHocEmails");

            migrationBuilder.CreateIndex(
                name: "IX_SentAdHocEmails_DeliveryStatus",
                schema: "public",
                table: "SentAdHocEmails",
                column: "DeliveryStatus",
                filter: "\"DeliveryStatus\" IN ('Pending', 'Failed')");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_SentAdHocEmails_DeliveryStatus",
                schema: "public",
                table: "SentAdHocEmails",
                sql: "\"DeliveryStatus\" IN ('Pending', 'Scheduled', 'Sent', 'Delivered', 'Failed', 'Bounced')");
        }
    }
}
