using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeRegistrationFieldsNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Make CancellationReason nullable
            migrationBuilder.AlterColumn<string>(
                name: "CancellationReason",
                table: "Registrations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            // Make DietaryRestrictions nullable
            migrationBuilder.AlterColumn<string>(
                name: "DietaryRestrictions",
                table: "Registrations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            // Make AccessibilityNeeds nullable
            migrationBuilder.AlterColumn<string>(
                name: "AccessibilityNeeds",
                table: "Registrations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            // Make EmergencyContactName nullable
            migrationBuilder.AlterColumn<string>(
                name: "EmergencyContactName",
                table: "Registrations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            // Make EmergencyContactPhone nullable
            migrationBuilder.AlterColumn<string>(
                name: "EmergencyContactPhone",
                table: "Registrations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert CancellationReason to non-nullable
            migrationBuilder.AlterColumn<string>(
                name: "CancellationReason",
                table: "Registrations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            // Revert DietaryRestrictions to non-nullable
            migrationBuilder.AlterColumn<string>(
                name: "DietaryRestrictions",
                table: "Registrations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            // Revert AccessibilityNeeds to non-nullable
            migrationBuilder.AlterColumn<string>(
                name: "AccessibilityNeeds",
                table: "Registrations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            // Revert EmergencyContactName to non-nullable
            migrationBuilder.AlterColumn<string>(
                name: "EmergencyContactName",
                table: "Registrations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            // Revert EmergencyContactPhone to non-nullable
            migrationBuilder.AlterColumn<string>(
                name: "EmergencyContactPhone",
                table: "Registrations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
