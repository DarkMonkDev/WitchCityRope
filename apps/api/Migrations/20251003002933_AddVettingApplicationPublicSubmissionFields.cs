using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVettingApplicationPublicSubmissionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "VettingApplications",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<bool>(
                name: "AgreesToGuidelines",
                table: "VettingApplications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AgreesToTerms",
                table: "VettingApplications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationNumber",
                table: "VettingApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "ConsentToContact",
                table: "VettingApplications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ConsentUnderstanding",
                table: "VettingApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpectationsGoals",
                table: "VettingApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExperienceDescription",
                table: "VettingApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExperienceLevel",
                table: "VettingApplications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "VettingApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymous",
                table: "VettingApplications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReviewedAt",
                table: "VettingApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "VettingApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "References",
                table: "VettingApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SafetyKnowledge",
                table: "VettingApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SkillsInterests",
                table: "VettingApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusToken",
                table: "VettingApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WhyJoinCommunity",
                table: "VettingApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearsExperience",
                table: "VettingApplications",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgreesToGuidelines",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "AgreesToTerms",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "ApplicationNumber",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "ConsentToContact",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "ConsentUnderstanding",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "ExpectationsGoals",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "ExperienceDescription",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "ExperienceLevel",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "IsAnonymous",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "LastReviewedAt",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "References",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "SafetyKnowledge",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "SkillsInterests",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "StatusToken",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "WhyJoinCommunity",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "YearsExperience",
                table: "VettingApplications");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "VettingApplications",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
