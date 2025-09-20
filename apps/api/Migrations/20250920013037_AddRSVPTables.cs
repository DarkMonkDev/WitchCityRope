using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRSVPTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventParticipations",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipationType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventParticipations", x => x.Id);
                    table.CheckConstraint("CHK_EventParticipations_CancelledAt_Logic", "(\"Status\" IN (2, 3) AND \"CancelledAt\" IS NOT NULL) OR (\"Status\" NOT IN (2, 3) AND \"CancelledAt\" IS NULL)");
                    table.CheckConstraint("CHK_EventParticipations_ParticipationType", "\"ParticipationType\" IN (1, 2)");
                    table.CheckConstraint("CHK_EventParticipations_Status", "\"Status\" IN (1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "FK_EventParticipations_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventParticipations_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventParticipations_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventParticipations_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParticipationHistory",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OldValues = table.Column<string>(type: "jsonb", nullable: true),
                    NewValues = table.Column<string>(type: "jsonb", nullable: true),
                    ChangedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangeReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipationHistory", x => x.Id);
                    table.CheckConstraint("CHK_ParticipationHistory_ActionType", "\"ActionType\" IN ('Created', 'Updated', 'Cancelled', 'Refunded', 'StatusChanged', 'PaymentUpdated')");
                    table.ForeignKey(
                        name: "FK_ParticipationHistory_EventParticipations_ParticipationId",
                        column: x => x.ParticipationId,
                        principalSchema: "public",
                        principalTable: "EventParticipations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParticipationHistory_Users_ChangedBy",
                        column: x => x.ChangedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipations_CreatedAt",
                schema: "public",
                table: "EventParticipations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipations_CreatedBy",
                schema: "public",
                table: "EventParticipations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipations_EventId_Status",
                schema: "public",
                table: "EventParticipations",
                columns: new[] { "EventId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipations_Metadata_Gin",
                schema: "public",
                table: "EventParticipations",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipations_UpdatedBy",
                schema: "public",
                table: "EventParticipations",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipations_UserId_Status",
                schema: "public",
                table: "EventParticipations",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_EventParticipations_User_Event_Active",
                schema: "public",
                table: "EventParticipations",
                columns: new[] { "UserId", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParticipationHistory_ActionType",
                schema: "public",
                table: "ParticipationHistory",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipationHistory_ChangedBy",
                schema: "public",
                table: "ParticipationHistory",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipationHistory_NewValues_Gin",
                schema: "public",
                table: "ParticipationHistory",
                column: "NewValues")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipationHistory_OldValues_Gin",
                schema: "public",
                table: "ParticipationHistory",
                column: "OldValues")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipationHistory_ParticipationId_CreatedAt",
                schema: "public",
                table: "ParticipationHistory",
                columns: new[] { "ParticipationId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParticipationHistory",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EventParticipations",
                schema: "public");
        }
    }
}
