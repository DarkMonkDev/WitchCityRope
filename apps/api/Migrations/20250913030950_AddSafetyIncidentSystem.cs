using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSafetyIncidentSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create sequence for reference number generation
            migrationBuilder.Sql(@"
                CREATE SEQUENCE IF NOT EXISTS ""SafetyIncidentSequence"" 
                    START WITH 1
                    INCREMENT BY 1
                    MINVALUE 1
                    MAXVALUE 9999
                    CYCLE;
            ");

            // Create reference number generation function
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION generate_safety_reference_number()
                RETURNS VARCHAR(20) AS $$
                DECLARE
                    date_part VARCHAR(8);
                    sequence_part VARCHAR(4);
                    reference_number VARCHAR(20);
                    max_attempts INTEGER := 100;
                    attempt_count INTEGER := 0;
                BEGIN
                    date_part := TO_CHAR(NOW(), 'YYYYMMDD');
                    
                    LOOP
                        sequence_part := LPAD(nextval('""SafetyIncidentSequence""')::TEXT, 4, '0');
                        reference_number := 'SAF-' || date_part || '-' || sequence_part;
                        
                        IF NOT EXISTS (
                            SELECT 1 FROM ""SafetyIncidents"" 
                            WHERE ""ReferenceNumber"" = reference_number
                        ) THEN
                            RETURN reference_number;
                        END IF;
                        
                        attempt_count := attempt_count + 1;
                        IF attempt_count >= max_attempts THEN
                            RAISE EXCEPTION 'Unable to generate unique reference number after % attempts', max_attempts;
                        END IF;
                    END LOOP;
                END;
                $$ LANGUAGE plpgsql;
            ");
            migrationBuilder.CreateTable(
                name: "SafetyIncidents",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: true),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    IncidentDate = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EncryptedDescription = table.Column<string>(type: "text", nullable: false),
                    EncryptedInvolvedParties = table.Column<string>(type: "text", nullable: true),
                    EncryptedWitnesses = table.Column<string>(type: "text", nullable: true),
                    EncryptedContactEmail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EncryptedContactPhone = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsAnonymous = table.Column<bool>(type: "boolean", nullable: false),
                    RequestFollowUp = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AssignedTo = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SafetyIncidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SafetyIncidents_Users_AssignedTo",
                        column: x => x.AssignedTo,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SafetyIncidents_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SafetyIncidents_Users_ReporterId",
                        column: x => x.ReporterId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SafetyIncidents_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "IncidentAuditLog",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActionDescription = table.Column<string>(type: "text", nullable: false),
                    OldValues = table.Column<string>(type: "jsonb", nullable: true),
                    NewValues = table.Column<string>(type: "jsonb", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentAuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentAuditLog_SafetyIncidents_IncidentId",
                        column: x => x.IncidentId,
                        principalSchema: "public",
                        principalTable: "SafetyIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IncidentAuditLog_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "IncidentNotifications",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RecipientType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RecipientEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MessageBody = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentNotifications_SafetyIncidents_IncidentId",
                        column: x => x.IncidentId,
                        principalSchema: "public",
                        principalTable: "SafetyIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentAuditLog_ActionType",
                schema: "public",
                table: "IncidentAuditLog",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentAuditLog_CreatedAt",
                schema: "public",
                table: "IncidentAuditLog",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentAuditLog_IncidentId_CreatedAt",
                schema: "public",
                table: "IncidentAuditLog",
                columns: new[] { "IncidentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentAuditLog_NewValues",
                schema: "public",
                table: "IncidentAuditLog",
                column: "NewValues")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentAuditLog_OldValues",
                schema: "public",
                table: "IncidentAuditLog",
                column: "OldValues")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentAuditLog_UserId",
                schema: "public",
                table: "IncidentAuditLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentNotifications_Failed_RetryCount",
                schema: "public",
                table: "IncidentNotifications",
                columns: new[] { "CreatedAt", "RetryCount" },
                filter: "\"Status\" = 'Failed' AND \"RetryCount\" < 5");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentNotifications_IncidentId",
                schema: "public",
                table: "IncidentNotifications",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentNotifications_RecipientType",
                schema: "public",
                table: "IncidentNotifications",
                column: "RecipientType");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentNotifications_Status_CreatedAt",
                schema: "public",
                table: "IncidentNotifications",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidents_AssignedTo",
                schema: "public",
                table: "SafetyIncidents",
                column: "AssignedTo");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidents_CreatedBy",
                schema: "public",
                table: "SafetyIncidents",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidents_ReferenceNumber",
                schema: "public",
                table: "SafetyIncidents",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidents_ReportedAt",
                schema: "public",
                table: "SafetyIncidents",
                column: "ReportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidents_ReporterId",
                schema: "public",
                table: "SafetyIncidents",
                column: "ReporterId",
                filter: "\"ReporterId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidents_Severity",
                schema: "public",
                table: "SafetyIncidents",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidents_Status",
                schema: "public",
                table: "SafetyIncidents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidents_Status_Severity_ReportedAt",
                schema: "public",
                table: "SafetyIncidents",
                columns: new[] { "Status", "Severity", "ReportedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SafetyIncidents_UpdatedBy",
                schema: "public",
                table: "SafetyIncidents",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IncidentAuditLog",
                schema: "public");

            migrationBuilder.DropTable(
                name: "IncidentNotifications",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SafetyIncidents",
                schema: "public");

            // Drop function and sequence
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS generate_safety_reference_number();");
            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS \"SafetyIncidentSequence\";");
        }
    }
}
