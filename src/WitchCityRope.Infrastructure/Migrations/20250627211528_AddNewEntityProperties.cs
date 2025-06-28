using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNewEntityProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConsentUnderstanding",
                table: "VettingApplications",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExperienceDescription",
                table: "VettingApplications",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhyJoin",
                table: "VettingApplications",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVetted",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PronouncedName",
                table: "Users",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pronouns",
                table: "Users",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckedInAt",
                table: "Registrations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CheckedInBy",
                table: "Registrations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmationCode",
                table: "Registrations",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Registrations",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactName",
                table: "Registrations",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactPhone",
                table: "Registrations",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RecommendAction",
                table: "IncidentReviews",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedToId",
                table: "IncidentReports",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IncidentDate",
                table: "IncidentReports",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "IncidentType",
                table: "IncidentReports",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "IncidentReports",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PreferredContactMethod",
                table: "IncidentReports",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "IncidentReports",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RequestFollowUp",
                table: "IncidentReports",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsVetted",
                table: "Users",
                column: "IsVetted");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_CheckedInAt",
                table: "Registrations",
                column: "CheckedInAt");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_ConfirmationCode",
                table: "Registrations",
                column: "ConfirmationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_AssignedToId",
                table: "IncidentReports",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_IncidentDate",
                table: "IncidentReports",
                column: "IncidentDate");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_IncidentType",
                table: "IncidentReports",
                column: "IncidentType");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_ReferenceNumber",
                table: "IncidentReports",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_IncidentReports_Users_AssignedToId",
                table: "IncidentReports",
                column: "AssignedToId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IncidentReports_Users_AssignedToId",
                table: "IncidentReports");

            migrationBuilder.DropIndex(
                name: "IX_Users_IsVetted",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_CheckedInAt",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_ConfirmationCode",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_IncidentReports_AssignedToId",
                table: "IncidentReports");

            migrationBuilder.DropIndex(
                name: "IX_IncidentReports_IncidentDate",
                table: "IncidentReports");

            migrationBuilder.DropIndex(
                name: "IX_IncidentReports_IncidentType",
                table: "IncidentReports");

            migrationBuilder.DropIndex(
                name: "IX_IncidentReports_ReferenceNumber",
                table: "IncidentReports");

            migrationBuilder.DropColumn(
                name: "ConsentUnderstanding",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "ExperienceDescription",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "WhyJoin",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "IsVetted",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PronouncedName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Pronouns",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CheckedInAt",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "CheckedInBy",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "ConfirmationCode",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "EmergencyContactName",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "EmergencyContactPhone",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "RecommendAction",
                table: "IncidentReviews");

            migrationBuilder.DropColumn(
                name: "AssignedToId",
                table: "IncidentReports");

            migrationBuilder.DropColumn(
                name: "IncidentDate",
                table: "IncidentReports");

            migrationBuilder.DropColumn(
                name: "IncidentType",
                table: "IncidentReports");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "IncidentReports");

            migrationBuilder.DropColumn(
                name: "PreferredContactMethod",
                table: "IncidentReports");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "IncidentReports");

            migrationBuilder.DropColumn(
                name: "RequestFollowUp",
                table: "IncidentReports");
        }
    }
}
