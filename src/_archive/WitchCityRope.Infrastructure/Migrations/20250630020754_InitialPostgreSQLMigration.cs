using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSQLMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PricingTiers = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedLegalName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SceneName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PronouncedName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Pronouns = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsVetted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventOrganizers",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventOrganizers", x => new { x.EventId, x.UserId });
                    table.ForeignKey(
                        name: "FK_EventOrganizers_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventOrganizers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IncidentReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    IsAnonymous = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IncidentType = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IncidentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequestFollowUp = table.Column<bool>(type: "boolean", nullable: false),
                    PreferredContactMethod = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AssignedToId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentReports_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncidentReports_Users_AssignedToId",
                        column: x => x.AssignedToId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncidentReports_Users_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectedPriceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SelectedPriceCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DietaryRestrictions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AccessibilityNeeds = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EmergencyContactName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EmergencyContactPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CheckedInAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckedInBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmationCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Registrations_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Registrations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserAuthentications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TwoFactorSecret = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsTwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastPasswordChangeAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "integer", nullable: false),
                    LockedOutUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAuthentications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAuthentications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VettingApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExperienceLevel = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Interests = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SafetyKnowledge = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ExperienceDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ConsentUnderstanding = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    WhyJoin = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecisionNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    References = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VettingApplications_Users_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IncidentActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PerformedById = table.Column<Guid>(type: "uuid", nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IncidentReportId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentActions_IncidentReports_IncidentReportId",
                        column: x => x.IncidentReportId,
                        principalTable: "IncidentReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IncidentActions_Users_PerformedById",
                        column: x => x.PerformedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IncidentReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Findings = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RecommendedSeverity = table.Column<string>(type: "text", nullable: false),
                    RecommendAction = table.Column<bool>(type: "boolean", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IncidentReportId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentReviews_IncidentReports_IncidentReportId",
                        column: x => x.IncidentReportId,
                        principalTable: "IncidentReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IncidentReviews_Users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RefundAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    RefundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefundTransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RefundReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Registrations_RegistrationId",
                        column: x => x.RegistrationId,
                        principalTable: "Registrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VettingReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Recommendation = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VettingApplicationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VettingReviews_Users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VettingReviews_VettingApplications_VettingApplicationId",
                        column: x => x.VettingApplicationId,
                        principalTable: "VettingApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventOrganizers_UserId",
                table: "EventOrganizers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventType",
                table: "Events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_Events_IsPublished",
                table: "Events",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_Events_IsPublished_StartDate",
                table: "Events",
                columns: new[] { "IsPublished", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_StartDate",
                table: "Events",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentActions_IncidentReportId",
                table: "IncidentActions",
                column: "IncidentReportId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentActions_PerformedById",
                table: "IncidentActions",
                column: "PerformedById");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_AssignedToId",
                table: "IncidentReports",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_EventId",
                table: "IncidentReports",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_IncidentDate",
                table: "IncidentReports",
                column: "IncidentDate");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_IncidentType",
                table: "IncidentReports",
                column: "IncidentType");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_IsAnonymous",
                table: "IncidentReports",
                column: "IsAnonymous");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_ReferenceNumber",
                table: "IncidentReports",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_ReportedAt",
                table: "IncidentReports",
                column: "ReportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_ReporterId",
                table: "IncidentReports",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_Severity",
                table: "IncidentReports",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_Status",
                table: "IncidentReports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReviews_IncidentReportId",
                table: "IncidentReviews",
                column: "IncidentReportId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReviews_ReviewerId",
                table: "IncidentReviews",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProcessedAt",
                table: "Payments",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RefundTransactionId",
                table: "Payments",
                column: "RefundTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RegistrationId",
                table: "Payments",
                column: "RegistrationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                table: "Payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TransactionId",
                table: "Payments",
                column: "TransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

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
                name: "IX_Registrations_EventId",
                table: "Registrations",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_RegisteredAt",
                table: "Registrations",
                column: "RegisteredAt");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_Status",
                table: "Registrations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_UserId",
                table: "Registrations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_UserId_EventId",
                table: "Registrations",
                columns: new[] { "UserId", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthentications_UserId",
                table: "UserAuthentications",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsVetted",
                table: "Users",
                column: "IsVetted");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SceneName",
                table: "Users",
                column: "SceneName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_ApplicantId",
                table: "VettingApplications",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_Status",
                table: "VettingApplications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_SubmittedAt",
                table: "VettingApplications",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VettingReviews_ReviewerId",
                table: "VettingReviews",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingReviews_VettingApplicationId",
                table: "VettingReviews",
                column: "VettingApplicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventOrganizers");

            migrationBuilder.DropTable(
                name: "IncidentActions");

            migrationBuilder.DropTable(
                name: "IncidentReviews");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "UserAuthentications");

            migrationBuilder.DropTable(
                name: "VettingReviews");

            migrationBuilder.DropTable(
                name: "IncidentReports");

            migrationBuilder.DropTable(
                name: "Registrations");

            migrationBuilder.DropTable(
                name: "VettingApplications");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
