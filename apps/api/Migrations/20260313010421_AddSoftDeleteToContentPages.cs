using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteToContentPages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "public",
                table: "ContentPages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                schema: "public",
                table: "ContentPages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "public",
                table: "ContentPages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ContentPages_DeletedBy",
                schema: "public",
                table: "ContentPages",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPages_IsDeleted",
                schema: "public",
                table: "ContentPages",
                column: "IsDeleted",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_ContentPages_DeletedBy",
                schema: "public",
                table: "ContentPages",
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
                name: "FK_ContentPages_DeletedBy",
                schema: "public",
                table: "ContentPages");

            migrationBuilder.DropIndex(
                name: "IX_ContentPages_DeletedBy",
                schema: "public",
                table: "ContentPages");

            migrationBuilder.DropIndex(
                name: "IX_ContentPages_IsDeleted",
                schema: "public",
                table: "ContentPages");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "public",
                table: "ContentPages");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "public",
                table: "ContentPages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "public",
                table: "ContentPages");
        }
    }
}
