using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVettingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VettingReviewers",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MaxWorkload = table.Column<int>(type: "integer", nullable: false),
                    CurrentWorkload = table.Column<int>(type: "integer", nullable: false),
                    SpecializationsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    TotalReviewsCompleted = table.Column<int>(type: "integer", nullable: false),
                    AverageReviewTimeHours = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ApprovalRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    UnavailableUntil = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    TimeZone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "UTC"),
                    WorkingHoursJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingReviewers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VettingReviewers_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VettingApplications",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StatusToken = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EncryptedFullName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EncryptedSceneName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EncryptedPronouns = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EncryptedEmail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EncryptedPhone = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExperienceLevel = table.Column<int>(type: "integer", nullable: false),
                    YearsExperience = table.Column<int>(type: "integer", nullable: false),
                    EncryptedExperienceDescription = table.Column<string>(type: "text", nullable: false),
                    EncryptedSafetyKnowledge = table.Column<string>(type: "text", nullable: false),
                    EncryptedConsentUnderstanding = table.Column<string>(type: "text", nullable: false),
                    EncryptedWhyJoinCommunity = table.Column<string>(type: "text", nullable: false),
                    SkillsInterests = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    EncryptedExpectationsGoals = table.Column<string>(type: "text", nullable: false),
                    AgreesToGuidelines = table.Column<bool>(type: "boolean", nullable: false),
                    IsAnonymous = table.Column<bool>(type: "boolean", nullable: false),
                    AgreesToTerms = table.Column<bool>(type: "boolean", nullable: false),
                    ConsentToContact = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedReviewerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    ReviewStartedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    DecisionMadeAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    InterviewScheduledFor = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VettingApplications_Users_ApplicantId",
                        column: x => x.ApplicantId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VettingApplications_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VettingApplications_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VettingApplications_VettingReviewers_AssignedReviewerId",
                        column: x => x.AssignedReviewerId,
                        principalSchema: "public",
                        principalTable: "VettingReviewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VettingApplicationAuditLog",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActionDescription = table.Column<string>(type: "text", nullable: false),
                    OldValues = table.Column<string>(type: "jsonb", nullable: true),
                    NewValues = table.Column<string>(type: "jsonb", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingApplicationAuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VettingApplicationAuditLog_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VettingApplicationAuditLog_VettingApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "public",
                        principalTable: "VettingApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VettingApplicationNotes",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsPrivate = table.Column<bool>(type: "boolean", nullable: false),
                    TagsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingApplicationNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VettingApplicationNotes_VettingApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "public",
                        principalTable: "VettingApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VettingApplicationNotes_VettingReviewers_ReviewerId",
                        column: x => x.ReviewerId,
                        principalSchema: "public",
                        principalTable: "VettingReviewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VettingDecisions",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionType = table.Column<int>(type: "integer", nullable: false),
                    Reasoning = table.Column<string>(type: "text", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: true),
                    IsFinalDecision = table.Column<bool>(type: "boolean", nullable: false),
                    AdditionalInfoRequested = table.Column<string>(type: "text", nullable: true),
                    AdditionalInfoDeadline = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    ProposedInterviewTime = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    InterviewNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    DecisionIpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    DecisionUserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VettingDecisions_VettingApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "public",
                        principalTable: "VettingApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VettingDecisions_VettingReviewers_ReviewerId",
                        column: x => x.ReviewerId,
                        principalSchema: "public",
                        principalTable: "VettingReviewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VettingNotifications",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationType = table.Column<int>(type: "integer", nullable: false),
                    RecipientEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MessageBody = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    NextRetryAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    MessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeliveryStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TemplateName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TemplateData = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VettingNotifications_VettingApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "public",
                        principalTable: "VettingApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VettingReferences",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceOrder = table.Column<int>(type: "integer", nullable: false),
                    EncryptedName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EncryptedEmail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EncryptedRelationship = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ResponseToken = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ContactedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    FirstReminderSentAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    SecondReminderSentAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    FinalReminderSentAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    FormExpiresAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    RequiresManualContact = table.Column<bool>(type: "boolean", nullable: false),
                    ManualContactNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ManualContactAttemptAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VettingReferences_VettingApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalSchema: "public",
                        principalTable: "VettingApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VettingNoteAttachments",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileExtension = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsConfidential = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingNoteAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VettingNoteAttachments_VettingApplicationNotes_NoteId",
                        column: x => x.NoteId,
                        principalSchema: "public",
                        principalTable: "VettingApplicationNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VettingDecisionAuditLog",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActionDescription = table.Column<string>(type: "text", nullable: false),
                    PreviousDecision = table.Column<string>(type: "text", nullable: true),
                    NewDecision = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingDecisionAuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VettingDecisionAuditLog_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VettingDecisionAuditLog_VettingDecisions_DecisionId",
                        column: x => x.DecisionId,
                        principalSchema: "public",
                        principalTable: "VettingDecisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VettingReferenceAuditLog",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActionDescription = table.Column<string>(type: "text", nullable: false),
                    EmailMessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeliveryStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingReferenceAuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VettingReferenceAuditLog_VettingReferences_ReferenceId",
                        column: x => x.ReferenceId,
                        principalSchema: "public",
                        principalTable: "VettingReferences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VettingReferenceResponses",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedRelationshipDuration = table.Column<string>(type: "text", nullable: false),
                    EncryptedExperienceAssessment = table.Column<string>(type: "text", nullable: false),
                    EncryptedSafetyConcerns = table.Column<string>(type: "text", nullable: true),
                    EncryptedCommunityReadiness = table.Column<string>(type: "text", nullable: false),
                    Recommendation = table.Column<int>(type: "integer", nullable: false),
                    EncryptedAdditionalComments = table.Column<string>(type: "text", nullable: true),
                    ResponseIpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    ResponseUserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingReferenceResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VettingReferenceResponses_VettingReferences_ReferenceId",
                        column: x => x.ReferenceId,
                        principalSchema: "public",
                        principalTable: "VettingReferences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplicationAuditLog_ActionType",
                schema: "public",
                table: "VettingApplicationAuditLog",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplicationAuditLog_ApplicationId_CreatedAt",
                schema: "public",
                table: "VettingApplicationAuditLog",
                columns: new[] { "ApplicationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplicationAuditLog_CreatedAt",
                schema: "public",
                table: "VettingApplicationAuditLog",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplicationAuditLog_NewValues",
                schema: "public",
                table: "VettingApplicationAuditLog",
                column: "NewValues")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplicationAuditLog_OldValues",
                schema: "public",
                table: "VettingApplicationAuditLog",
                column: "OldValues")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplicationAuditLog_UserId",
                schema: "public",
                table: "VettingApplicationAuditLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplicationNotes_ApplicationId",
                schema: "public",
                table: "VettingApplicationNotes",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplicationNotes_ApplicationId_Type_CreatedAt",
                schema: "public",
                table: "VettingApplicationNotes",
                columns: new[] { "ApplicationId", "Type", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplicationNotes_IsPrivate",
                schema: "public",
                table: "VettingApplicationNotes",
                column: "IsPrivate");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplicationNotes_ReviewerId",
                schema: "public",
                table: "VettingApplicationNotes",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplicationNotes_Tags",
                schema: "public",
                table: "VettingApplicationNotes",
                column: "TagsJson")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_Active_Status_CreatedAt",
                schema: "public",
                table: "VettingApplications",
                columns: new[] { "Status", "CreatedAt" },
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_ApplicantId",
                schema: "public",
                table: "VettingApplications",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_ApplicationNumber",
                schema: "public",
                table: "VettingApplications",
                column: "ApplicationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_AssignedReviewerId",
                schema: "public",
                table: "VettingApplications",
                column: "AssignedReviewerId",
                filter: "\"AssignedReviewerId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_CreatedBy",
                schema: "public",
                table: "VettingApplications",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_DeletedAt",
                schema: "public",
                table: "VettingApplications",
                column: "DeletedAt",
                filter: "\"DeletedAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_SkillsInterests",
                schema: "public",
                table: "VettingApplications",
                column: "SkillsInterests")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_Status",
                schema: "public",
                table: "VettingApplications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_Status_Priority_CreatedAt",
                schema: "public",
                table: "VettingApplications",
                columns: new[] { "Status", "Priority", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_StatusToken",
                schema: "public",
                table: "VettingApplications",
                column: "StatusToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_UpdatedBy",
                schema: "public",
                table: "VettingApplications",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VettingDecisionAuditLog_ActionType",
                schema: "public",
                table: "VettingDecisionAuditLog",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_VettingDecisionAuditLog_ApplicationId",
                schema: "public",
                table: "VettingDecisionAuditLog",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingDecisionAuditLog_DecisionId_CreatedAt",
                schema: "public",
                table: "VettingDecisionAuditLog",
                columns: new[] { "DecisionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VettingDecisionAuditLog_UserId",
                schema: "public",
                table: "VettingDecisionAuditLog",
                column: "UserId",
                filter: "\"UserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VettingDecisions_ApplicationId",
                schema: "public",
                table: "VettingDecisions",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingDecisions_ApplicationId_CreatedAt",
                schema: "public",
                table: "VettingDecisions",
                columns: new[] { "ApplicationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VettingDecisions_DecisionType",
                schema: "public",
                table: "VettingDecisions",
                column: "DecisionType");

            migrationBuilder.CreateIndex(
                name: "IX_VettingDecisions_IsFinalDecision",
                schema: "public",
                table: "VettingDecisions",
                column: "IsFinalDecision",
                filter: "\"IsFinalDecision\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_VettingDecisions_ReviewerId",
                schema: "public",
                table: "VettingDecisions",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingNoteAttachments_FileHash",
                schema: "public",
                table: "VettingNoteAttachments",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_VettingNoteAttachments_IsConfidential",
                schema: "public",
                table: "VettingNoteAttachments",
                column: "IsConfidential");

            migrationBuilder.CreateIndex(
                name: "IX_VettingNoteAttachments_NoteId",
                schema: "public",
                table: "VettingNoteAttachments",
                column: "NoteId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingNoteAttachments_NoteId_FileName",
                schema: "public",
                table: "VettingNoteAttachments",
                columns: new[] { "NoteId", "FileName" });

            migrationBuilder.CreateIndex(
                name: "IX_VettingNotifications_ApplicationId",
                schema: "public",
                table: "VettingNotifications",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingNotifications_DeliveryQueue",
                schema: "public",
                table: "VettingNotifications",
                columns: new[] { "Status", "NextRetryAt", "CreatedAt" },
                filter: "\"Status\" IN (1, 4)");

            migrationBuilder.CreateIndex(
                name: "IX_VettingNotifications_Failed_RetryCount",
                schema: "public",
                table: "VettingNotifications",
                columns: new[] { "CreatedAt", "RetryCount" },
                filter: "\"Status\" = 4 AND \"RetryCount\" < 5");

            migrationBuilder.CreateIndex(
                name: "IX_VettingNotifications_NotificationType",
                schema: "public",
                table: "VettingNotifications",
                column: "NotificationType");

            migrationBuilder.CreateIndex(
                name: "IX_VettingNotifications_Status_CreatedAt",
                schema: "public",
                table: "VettingNotifications",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VettingNotifications_TemplateData",
                schema: "public",
                table: "VettingNotifications",
                column: "TemplateData")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_VettingReferenceAuditLog_ActionType",
                schema: "public",
                table: "VettingReferenceAuditLog",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_VettingReferenceAuditLog_ApplicationId",
                schema: "public",
                table: "VettingReferenceAuditLog",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingReferenceAuditLog_EmailMessageId",
                schema: "public",
                table: "VettingReferenceAuditLog",
                column: "EmailMessageId",
                filter: "\"EmailMessageId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VettingReferenceAuditLog_ReferenceId_CreatedAt",
                schema: "public",
                table: "VettingReferenceAuditLog",
                columns: new[] { "ReferenceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VettingReferenceResponses_IsCompleted",
                schema: "public",
                table: "VettingReferenceResponses",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_VettingReferenceResponses_Recommendation",
                schema: "public",
                table: "VettingReferenceResponses",
                column: "Recommendation");

            migrationBuilder.CreateIndex(
                name: "IX_VettingReferenceResponses_ReferenceId",
                schema: "public",
                table: "VettingReferenceResponses",
                column: "ReferenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VettingReferences_ApplicationId",
                schema: "public",
                table: "VettingReferences",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingReferences_ApplicationId_ReferenceOrder",
                schema: "public",
                table: "VettingReferences",
                columns: new[] { "ApplicationId", "ReferenceOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VettingReferences_ResponseToken",
                schema: "public",
                table: "VettingReferences",
                column: "ResponseToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VettingReferences_Status",
                schema: "public",
                table: "VettingReferences",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VettingReferences_StatusProcessing",
                schema: "public",
                table: "VettingReferences",
                columns: new[] { "Status", "ContactedAt", "FormExpiresAt" },
                filter: "\"Status\" IN (1, 2, 3)");

            migrationBuilder.CreateIndex(
                name: "IX_VettingReviewers_IsActive",
                schema: "public",
                table: "VettingReviewers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_VettingReviewers_Specializations",
                schema: "public",
                table: "VettingReviewers",
                column: "SpecializationsJson")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_VettingReviewers_UserId",
                schema: "public",
                table: "VettingReviewers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VettingReviewers_Workload",
                schema: "public",
                table: "VettingReviewers",
                columns: new[] { "IsActive", "IsAvailable", "CurrentWorkload", "MaxWorkload" },
                filter: "\"IsActive\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VettingApplicationAuditLog",
                schema: "public");

            migrationBuilder.DropTable(
                name: "VettingDecisionAuditLog",
                schema: "public");

            migrationBuilder.DropTable(
                name: "VettingNoteAttachments",
                schema: "public");

            migrationBuilder.DropTable(
                name: "VettingNotifications",
                schema: "public");

            migrationBuilder.DropTable(
                name: "VettingReferenceAuditLog",
                schema: "public");

            migrationBuilder.DropTable(
                name: "VettingReferenceResponses",
                schema: "public");

            migrationBuilder.DropTable(
                name: "VettingDecisions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "VettingApplicationNotes",
                schema: "public");

            migrationBuilder.DropTable(
                name: "VettingReferences",
                schema: "public");

            migrationBuilder.DropTable(
                name: "VettingApplications",
                schema: "public");

            migrationBuilder.DropTable(
                name: "VettingReviewers",
                schema: "public");
        }
    }
}
