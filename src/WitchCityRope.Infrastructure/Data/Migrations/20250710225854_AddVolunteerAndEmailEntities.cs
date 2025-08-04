using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVolunteerAndEmailEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Events",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "SkillLevel",
                table: "Events",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "RequiresVetting",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddColumn<decimal>(
                name: "CouplesPrice",
                table: "Events",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Events",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IndividualPrice",
                table: "Events",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RefundCutoffHours",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 48);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegistrationClosesAt",
                table: "Events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegistrationOpensAt",
                table: "Events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Events",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TicketTypes",
                table: "Events",
                type: "text",
                nullable: false,
                defaultValue: "Individual");

            migrationBuilder.CreateTable(
                name: "EventEmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventEmailTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventEmailTemplates_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    RequiredVolunteers = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VolunteerTasks_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    HasTicket = table.Column<bool>(type: "boolean", nullable: false),
                    TicketPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BackgroundCheckVerified = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VolunteerAssignments_VolunteerTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "VolunteerTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventEmailTemplates_EventId",
                table: "EventEmailTemplates",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventEmailTemplates_EventId_Type",
                table: "EventEmailTemplates",
                columns: new[] { "EventId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_EventEmailTemplates_IsActive",
                table: "EventEmailTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerAssignments_Status",
                table: "VolunteerAssignments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerAssignments_TaskId",
                table: "VolunteerAssignments",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerAssignments_TaskId_UserId",
                table: "VolunteerAssignments",
                columns: new[] { "TaskId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerAssignments_UserId",
                table: "VolunteerAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerTasks_EventId",
                table: "VolunteerTasks",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerTasks_EventId_StartTime",
                table: "VolunteerTasks",
                columns: new[] { "EventId", "StartTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventEmailTemplates");

            migrationBuilder.DropTable(
                name: "VolunteerAssignments");

            migrationBuilder.DropTable(
                name: "VolunteerTasks");

            migrationBuilder.DropColumn(
                name: "CouplesPrice",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "IndividualPrice",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RefundCutoffHours",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RegistrationClosesAt",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RegistrationOpensAt",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "TicketTypes",
                table: "Events");

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Events",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "SkillLevel",
                table: "Events",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "RequiresVetting",
                table: "Events",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);
        }
    }
}
