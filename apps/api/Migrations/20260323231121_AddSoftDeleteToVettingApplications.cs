using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteToVettingApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "VettingApplications",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "VettingApplications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "VettingApplications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_DeletedBy",
                table: "VettingApplications",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_IsDeleted",
                table: "VettingApplications",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_VettingApplications_Users_DeletedBy",
                table: "VettingApplications",
                column: "DeletedBy",
                principalSchema: "public",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VettingApplications_Users_DeletedBy",
                table: "VettingApplications");

            migrationBuilder.DropIndex(
                name: "IX_VettingApplications_DeletedBy",
                table: "VettingApplications");

            migrationBuilder.DropIndex(
                name: "IX_VettingApplications_IsDeleted",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "VettingApplications");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "VettingApplications");
        }
    }
}
