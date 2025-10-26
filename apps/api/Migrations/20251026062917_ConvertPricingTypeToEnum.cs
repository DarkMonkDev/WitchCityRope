using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class ConvertPricingTypeToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing data to match enum string representation
            // "fixed" -> "Fixed", "sliding-scale" -> "SlidingScale"
            migrationBuilder.Sql(@"
                UPDATE ""public"".""TicketTypes""
                SET ""PricingType"" = 'Fixed'
                WHERE ""PricingType"" = 'fixed';
            ");

            migrationBuilder.Sql(@"
                UPDATE ""public"".""TicketTypes""
                SET ""PricingType"" = 'SlidingScale'
                WHERE ""PricingType"" = 'sliding-scale';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert enum string representation to original lowercase hyphenated values
            // "Fixed" -> "fixed", "SlidingScale" -> "sliding-scale"
            migrationBuilder.Sql(@"
                UPDATE ""public"".""TicketTypes""
                SET ""PricingType"" = 'fixed'
                WHERE ""PricingType"" = 'Fixed';
            ");

            migrationBuilder.Sql(@"
                UPDATE ""public"".""TicketTypes""
                SET ""PricingType"" = 'sliding-scale'
                WHERE ""PricingType"" = 'SlidingScale';
            ");
        }
    }
}
