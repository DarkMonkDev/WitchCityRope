using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCleanSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "Events",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    PricingTiers = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SceneName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    EncryptedLegalName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PronouncedName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Pronouns = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsVetted = table.Column<bool>(type: "boolean", nullable: false),
                    FailedLoginAttempts = table.Column<int>(type: "integer", nullable: false),
                    LockedOutUntil = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    LastPasswordChangeAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    EmailVerificationToken = table.Column<string>(type: "text", nullable: false),
                    EmailVerificationTokenCreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    VettingStatus = table.Column<int>(type: "integer", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    CurrentAttendees = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "public",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventAttendees",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationStatus = table.Column<string>(type: "text", nullable: false),
                    TicketNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WaitlistPosition = table.Column<int>(type: "integer", nullable: true),
                    IsFirstTime = table.Column<bool>(type: "boolean", nullable: false),
                    DietaryRestrictions = table.Column<string>(type: "text", nullable: true),
                    AccessibilityNeeds = table.Column<string>(type: "text", nullable: true),
                    EmergencyContactName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    HasCompletedWaiver = table.Column<bool>(type: "boolean", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventAttendees", x => x.Id);
                    table.CheckConstraint("CHK_EventAttendees_RegistrationStatus", "\"RegistrationStatus\" IN ('confirmed', 'waitlist', 'checked-in', 'no-show', 'cancelled')");
                    table.CheckConstraint("CHK_EventAttendees_WaitlistPosition", "\"WaitlistPosition\" > 0 OR \"WaitlistPosition\" IS NULL");
                    table.ForeignKey(
                        name: "FK_EventAttendees_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventAttendees_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventAttendees_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventAttendees_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventOrganizers",
                schema: "public",
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
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventOrganizers_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "OfflineSyncQueue",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActionData = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    LocalTimestamp = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    SyncStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflineSyncQueue", x => x.Id);
                    table.CheckConstraint("CHK_OfflineSyncQueue_ActionType", "\"ActionType\" IN ('check-in', 'manual-entry', 'status-update', 'capacity-override')");
                    table.CheckConstraint("CHK_OfflineSyncQueue_RetryCount", "\"RetryCount\" >= 0 AND \"RetryCount\" <= 10");
                    table.CheckConstraint("CHK_OfflineSyncQueue_SyncedAt", "(\"SyncStatus\" = 'completed' AND \"SyncedAt\" IS NOT NULL) OR (\"SyncStatus\" != 'completed' AND \"SyncedAt\" IS NULL)");
                    table.CheckConstraint("CHK_OfflineSyncQueue_SyncStatus", "\"SyncStatus\" IN ('pending', 'syncing', 'completed', 'failed', 'conflict')");
                    table.ForeignKey(
                        name: "FK_OfflineSyncQueue_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OfflineSyncQueue_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedStripePaymentMethodId = table.Column<string>(type: "text", nullable: false),
                    LastFourDigits = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    CardBrand = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExpiryMonth = table.Column<int>(type: "integer", nullable: false),
                    ExpiryYear = table.Column<int>(type: "integer", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.Id);
                    table.CheckConstraint("CHK_PaymentMethods_CardBrand", "\"CardBrand\" IN ('Visa', 'MasterCard', 'American Express', 'Discover', 'JCB', 'Diners Club')");
                    table.CheckConstraint("CHK_PaymentMethods_ExpiryMonth_Range", "\"ExpiryMonth\" >= 1 AND \"ExpiryMonth\" <= 12");
                    table.CheckConstraint("CHK_PaymentMethods_ExpiryYear_Future", "\"ExpiryYear\" >= EXTRACT(YEAR FROM NOW())");
                    table.CheckConstraint("CHK_PaymentMethods_LastFourDigits", "\"LastFourDigits\" ~ '^\\d{4}$'");
                    table.ForeignKey(
                        name: "FK_PaymentMethods_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventRegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountValue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    SlidingScalePercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0.00m),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaymentMethodType = table.Column<int>(type: "integer", nullable: false),
                    EncryptedPayPalOrderId = table.Column<string>(type: "text", nullable: true),
                    EncryptedPayPalPayerId = table.Column<string>(type: "text", nullable: true),
                    VenmoUsername = table.Column<string>(type: "text", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    RefundAmountValue = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    RefundCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    EncryptedPayPalRefundId = table.Column<string>(type: "text", nullable: true),
                    RefundReason = table.Column<string>(type: "text", nullable: true),
                    RefundedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false, defaultValueSql: "'{}'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.CheckConstraint("CHK_Payments_AmountValue_NonNegative", "\"AmountValue\" >= 0");
                    table.CheckConstraint("CHK_Payments_Currency_Valid", "\"Currency\" IN ('USD', 'EUR', 'GBP', 'CAD')");
                    table.CheckConstraint("CHK_Payments_CurrencyConsistency", "(\"RefundCurrency\" IS NULL) OR (\"RefundCurrency\" = \"Currency\")");
                    table.CheckConstraint("CHK_Payments_RefundAmount_NotExceedOriginal", "\"RefundAmountValue\" IS NULL OR \"RefundAmountValue\" <= \"AmountValue\"");
                    table.CheckConstraint("CHK_Payments_RefundCurrency_Valid", "\"RefundCurrency\" IS NULL OR \"RefundCurrency\" IN ('USD', 'EUR', 'GBP', 'CAD')");
                    table.CheckConstraint("CHK_Payments_RefundRequiresOriginalPayment", "(\"RefundAmountValue\" IS NULL AND \"RefundedAt\" IS NULL) OR (\"RefundAmountValue\" IS NOT NULL AND \"RefundedAt\" IS NOT NULL)");
                    table.CheckConstraint("CHK_Payments_SlidingScalePercentage_Range", "\"SlidingScalePercentage\" >= 0 AND \"SlidingScalePercentage\" <= 75.00");
                    table.ForeignKey(
                        name: "FK_Payments_Users_RefundedByUserId",
                        column: x => x.RefundedByUserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Payments_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "UserClaims",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                schema: "public",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "public",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "public",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                schema: "public",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VettingApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SceneName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RealName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FetLifeHandle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Pronouns = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OtherNames = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AboutYourself = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    HowFoundUs = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    AdminNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ReviewStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecisionMadeAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InterviewScheduledFor = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VettingApplications_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VettingBulkOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    PerformedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    Parameters = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    TotalItems = table.Column<int>(type: "integer", nullable: false),
                    SuccessCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FailureCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ErrorSummary = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingBulkOperations", x => x.Id);
                    table.CheckConstraint("CHK_VettingBulkOperations_Counts", "\"SuccessCount\" + \"FailureCount\" <= \"TotalItems\"");
                    table.CheckConstraint("CHK_VettingBulkOperations_TotalItems", "\"TotalItems\" > 0");
                    table.ForeignKey(
                        name: "FK_VettingBulkOperations_Users_PerformedBy",
                        column: x => x.PerformedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VettingEmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateType = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    LastModified = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingEmailTemplates", x => x.Id);
                    table.CheckConstraint("CHK_VettingEmailTemplates_Body_Length", "LENGTH(\"Body\") >= 10");
                    table.CheckConstraint("CHK_VettingEmailTemplates_Subject_Length", "LENGTH(\"Subject\") BETWEEN 5 AND 200");
                    table.ForeignKey(
                        name: "FK_VettingEmailTemplates_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TicketTypes",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Available = table.Column<int>(type: "integer", nullable: false),
                    Sold = table.Column<int>(type: "integer", nullable: false),
                    IsRsvpMode = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketTypes_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketTypes_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "public",
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerPositions",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SlotsNeeded = table.Column<int>(type: "integer", nullable: false),
                    SlotsFilled = table.Column<int>(type: "integer", nullable: false),
                    RequiresExperience = table.Column<bool>(type: "boolean", nullable: false),
                    Requirements = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VolunteerPositions_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VolunteerPositions_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "public",
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CheckInAuditLog",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventAttendeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActionDescription = table.Column<string>(type: "text", nullable: false),
                    OldValues = table.Column<string>(type: "jsonb", nullable: true),
                    NewValues = table.Column<string>(type: "jsonb", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckInAuditLog", x => x.Id);
                    table.CheckConstraint("CHK_CheckInAuditLog_ActionType", "\"ActionType\" IN ('check-in', 'manual-entry', 'capacity-override', 'status-change', 'data-update')");
                    table.ForeignKey(
                        name: "FK_CheckInAuditLog_EventAttendees_EventAttendeeId",
                        column: x => x.EventAttendeeId,
                        principalSchema: "public",
                        principalTable: "EventAttendees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CheckInAuditLog_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CheckInAuditLog_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CheckIns",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventAttendeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    StaffMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsManualEntry = table.Column<bool>(type: "boolean", nullable: false),
                    OverrideCapacity = table.Column<bool>(type: "boolean", nullable: false),
                    ManualEntryData = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckIns", x => x.Id);
                    table.CheckConstraint("CHK_CheckIns_ManualEntryData", "(\"IsManualEntry\" = true AND \"ManualEntryData\" IS NOT NULL) OR (\"IsManualEntry\" = false AND \"ManualEntryData\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_CheckIns_EventAttendees_EventAttendeeId",
                        column: x => x.EventAttendeeId,
                        principalSchema: "public",
                        principalTable: "EventAttendees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CheckIns_Events_EventId",
                        column: x => x.EventId,
                        principalSchema: "public",
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CheckIns_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CheckIns_Users_StaffMemberId",
                        column: x => x.StaffMemberId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateTable(
                name: "PaymentAuditLog",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActionDescription = table.Column<string>(type: "text", nullable: false),
                    OldValues = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    NewValues = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAuditLog", x => x.Id);
                    table.CheckConstraint("CHK_PaymentAuditLog_ActionType", "\"ActionType\" IN ('PaymentInitiated', 'PaymentProcessed', 'PaymentCompleted', 'PaymentFailed', 'PaymentRetried', 'RefundInitiated', 'RefundCompleted', 'RefundFailed', 'StatusChanged', 'MetadataUpdated', 'SystemAction')");
                    table.ForeignKey(
                        name: "FK_PaymentAuditLog_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "public",
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentAuditLog_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PaymentFailures",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FailureCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FailureMessage = table.Column<string>(type: "text", nullable: false),
                    EncryptedStripeErrorDetails = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FailedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentFailures", x => x.Id);
                    table.CheckConstraint("CHK_PaymentFailures_RetryCount_NonNegative", "\"RetryCount\" >= 0");
                    table.ForeignKey(
                        name: "FK_PaymentFailures_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "public",
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentRefunds",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefundAmountValue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    RefundCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    RefundReason = table.Column<string>(type: "text", nullable: false),
                    RefundStatus = table.Column<int>(type: "integer", nullable: false),
                    EncryptedPayPalRefundId = table.Column<string>(type: "text", nullable: true),
                    ProcessedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    Metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false, defaultValueSql: "'{}'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRefunds", x => x.Id);
                    table.CheckConstraint("CHK_PaymentRefunds_Currency", "\"RefundCurrency\" IN ('USD', 'EUR', 'GBP', 'CAD')");
                    table.CheckConstraint("CHK_PaymentRefunds_ReasonRequired", "LENGTH(TRIM(\"RefundReason\")) >= 10");
                    table.CheckConstraint("CHK_PaymentRefunds_RefundAmountValue_Positive", "\"RefundAmountValue\" > 0");
                    table.ForeignKey(
                        name: "FK_PaymentRefunds_Payments_OriginalPaymentId",
                        column: x => x.OriginalPaymentId,
                        principalSchema: "public",
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentRefunds_Users_ProcessedByUserId",
                        column: x => x.ProcessedByUserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateTable(
                name: "VettingAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PerformedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OldValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    NewValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VettingAuditLogs_Users_PerformedBy",
                        column: x => x.PerformedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VettingAuditLogs_VettingApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "VettingApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VettingBulkOperationItems",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BulkOperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    RetryAt = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingBulkOperationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VettingBulkOperationItems_VettingApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "VettingApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VettingBulkOperationItems_VettingBulkOperations_BulkOperati~",
                        column: x => x.BulkOperationId,
                        principalTable: "VettingBulkOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VettingBulkOperationLogs",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BulkOperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    LogLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    OperationStep = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    StackTrace = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingBulkOperationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VettingBulkOperationLogs_VettingApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "VettingApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VettingBulkOperationLogs_VettingBulkOperations_BulkOperatio~",
                        column: x => x.BulkOperationId,
                        principalTable: "VettingBulkOperations",
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
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecipientEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VettingNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VettingNotifications_VettingApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "VettingApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VettingNotifications_VettingEmailTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "VettingEmailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TicketPurchases",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PaymentStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketPurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketPurchases_TicketTypes_TicketTypeId",
                        column: x => x.TicketTypeId,
                        principalSchema: "public",
                        principalTable: "TicketTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketPurchases_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CheckInAuditLog_ActionType",
                schema: "public",
                table: "CheckInAuditLog",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInAuditLog_CreatedAt",
                schema: "public",
                table: "CheckInAuditLog",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInAuditLog_Event_Time",
                schema: "public",
                table: "CheckInAuditLog",
                columns: new[] { "EventId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CheckInAuditLog_EventAttendeeId",
                schema: "public",
                table: "CheckInAuditLog",
                column: "EventAttendeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInAuditLog_EventId",
                schema: "public",
                table: "CheckInAuditLog",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInAuditLog_NewValues",
                schema: "public",
                table: "CheckInAuditLog",
                column: "NewValues",
                filter: "\"NewValues\" IS NOT NULL")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInAuditLog_OldValues",
                schema: "public",
                table: "CheckInAuditLog",
                column: "OldValues",
                filter: "\"OldValues\" IS NOT NULL")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInAuditLog_User_Time",
                schema: "public",
                table: "CheckInAuditLog",
                columns: new[] { "CreatedBy", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_CapacityOverride",
                schema: "public",
                table: "CheckIns",
                column: "OverrideCapacity",
                filter: "\"OverrideCapacity\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_CheckInTime",
                schema: "public",
                table: "CheckIns",
                column: "CheckInTime");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_CreatedBy",
                schema: "public",
                table: "CheckIns",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_Event_Time",
                schema: "public",
                table: "CheckIns",
                columns: new[] { "EventId", "CheckInTime" });

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_EventId",
                schema: "public",
                table: "CheckIns",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_ManualEntry",
                schema: "public",
                table: "CheckIns",
                column: "IsManualEntry",
                filter: "\"IsManualEntry\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_ManualEntryData",
                schema: "public",
                table: "CheckIns",
                column: "ManualEntryData",
                filter: "\"ManualEntryData\" IS NOT NULL")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_StaffMemberId",
                schema: "public",
                table: "CheckIns",
                column: "StaffMemberId");

            migrationBuilder.CreateIndex(
                name: "UQ_CheckIns_EventAttendee",
                schema: "public",
                table: "CheckIns",
                column: "EventAttendeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_CreatedBy",
                schema: "public",
                table: "EventAttendees",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_Event_Status",
                schema: "public",
                table: "EventAttendees",
                columns: new[] { "EventId", "RegistrationStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_Event_Waitlist",
                schema: "public",
                table: "EventAttendees",
                columns: new[] { "EventId", "WaitlistPosition" },
                filter: "\"RegistrationStatus\" = 'waitlist'");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_EventId",
                schema: "public",
                table: "EventAttendees",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_FirstTime",
                schema: "public",
                table: "EventAttendees",
                column: "IsFirstTime",
                filter: "\"IsFirstTime\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_Metadata",
                schema: "public",
                table: "EventAttendees",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_RegistrationStatus",
                schema: "public",
                table: "EventAttendees",
                column: "RegistrationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_TicketNumber_Unique",
                schema: "public",
                table: "EventAttendees",
                column: "TicketNumber",
                unique: true,
                filter: "\"TicketNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_UpdatedBy",
                schema: "public",
                table: "EventAttendees",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_UserId",
                schema: "public",
                table: "EventAttendees",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EventAttendees_Waiver",
                schema: "public",
                table: "EventAttendees",
                column: "HasCompletedWaiver",
                filter: "\"HasCompletedWaiver\" = false");

            migrationBuilder.CreateIndex(
                name: "UQ_EventAttendees_EventUser",
                schema: "public",
                table: "EventAttendees",
                columns: new[] { "EventId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventOrganizers_UserId",
                schema: "public",
                table: "EventOrganizers",
                column: "UserId");

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
                name: "IX_OfflineSyncQueue_ActionData",
                schema: "public",
                table: "OfflineSyncQueue",
                column: "ActionData")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineSyncQueue_EventId",
                schema: "public",
                table: "OfflineSyncQueue",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineSyncQueue_Failed",
                schema: "public",
                table: "OfflineSyncQueue",
                columns: new[] { "RetryCount", "CreatedAt" },
                filter: "\"SyncStatus\" = 'failed' AND \"RetryCount\" < 5");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineSyncQueue_LocalTimestamp",
                schema: "public",
                table: "OfflineSyncQueue",
                column: "LocalTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineSyncQueue_Pending",
                schema: "public",
                table: "OfflineSyncQueue",
                column: "CreatedAt",
                filter: "\"SyncStatus\" = 'pending'");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineSyncQueue_SyncStatus",
                schema: "public",
                table: "OfflineSyncQueue",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineSyncQueue_UserId",
                schema: "public",
                table: "OfflineSyncQueue",
                column: "UserId");

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

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAuditLog_ActionType",
                schema: "public",
                table: "PaymentAuditLog",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAuditLog_FailedActions",
                schema: "public",
                table: "PaymentAuditLog",
                column: "CreatedAt",
                filter: "\"ActionType\" IN ('PaymentFailed', 'RefundFailed')");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAuditLog_NewValues_Gin",
                schema: "public",
                table: "PaymentAuditLog",
                column: "NewValues")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAuditLog_OldValues_Gin",
                schema: "public",
                table: "PaymentAuditLog",
                column: "OldValues")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAuditLog_PaymentId_CreatedAt",
                schema: "public",
                table: "PaymentAuditLog",
                columns: new[] { "PaymentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAuditLog_UserId_CreatedAt",
                schema: "public",
                table: "PaymentAuditLog",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentFailures_FailedAt",
                schema: "public",
                table: "PaymentFailures",
                column: "FailedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentFailures_FailureCode",
                schema: "public",
                table: "PaymentFailures",
                column: "FailureCode");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentFailures_PaymentId",
                schema: "public",
                table: "PaymentFailures",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentFailures_RetryCount",
                schema: "public",
                table: "PaymentFailures",
                column: "RetryCount",
                filter: "\"RetryCount\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_IsActive",
                schema: "public",
                table: "PaymentMethods",
                columns: new[] { "UserId", "IsActive" },
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_IsDefault",
                schema: "public",
                table: "PaymentMethods",
                columns: new[] { "UserId", "IsDefault" },
                filter: "\"IsDefault\" = true");

            migrationBuilder.CreateIndex(
                name: "UX_PaymentMethods_UserDefault",
                schema: "public",
                table: "PaymentMethods",
                column: "UserId",
                unique: true,
                filter: "\"IsDefault\" = true AND \"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_Metadata_Gin",
                schema: "public",
                table: "PaymentRefunds",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_OriginalPaymentId",
                schema: "public",
                table: "PaymentRefunds",
                column: "OriginalPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_ProcessedAt",
                schema: "public",
                table: "PaymentRefunds",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_ProcessedByUserId",
                schema: "public",
                table: "PaymentRefunds",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_RefundStatus",
                schema: "public",
                table: "PaymentRefunds",
                column: "RefundStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_FailedStatus",
                schema: "public",
                table: "Payments",
                column: "ProcessedAt",
                filter: "\"Status\" = 2");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Metadata_Gin",
                schema: "public",
                table: "Payments",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PendingStatus",
                schema: "public",
                table: "Payments",
                column: "CreatedAt",
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RefundedByUserId",
                schema: "public",
                table: "Payments",
                column: "RefundedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RefundedStatus",
                schema: "public",
                table: "Payments",
                column: "RefundedAt",
                filter: "\"RefundedAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SlidingScalePercentage",
                schema: "public",
                table: "Payments",
                column: "SlidingScalePercentage");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                schema: "public",
                table: "Payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UserId",
                schema: "public",
                table: "Payments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_Payments_EventRegistration_Completed",
                schema: "public",
                table: "Payments",
                column: "EventRegistrationId",
                unique: true,
                filter: "\"Status\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                schema: "public",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "public",
                table: "Roles",
                column: "NormalizedName",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_EventId",
                schema: "public",
                table: "Sessions",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_EventId_SessionCode",
                schema: "public",
                table: "Sessions",
                columns: new[] { "EventId", "SessionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketPurchases_PaymentStatus",
                schema: "public",
                table: "TicketPurchases",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_TicketPurchases_TicketTypeId",
                schema: "public",
                table: "TicketPurchases",
                column: "TicketTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketPurchases_UserId",
                schema: "public",
                table: "TicketPurchases",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTypes_EventId",
                schema: "public",
                table: "TicketTypes",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTypes_SessionId",
                schema: "public",
                table: "TicketTypes",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                schema: "public",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                schema: "public",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                schema: "public",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "public",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                schema: "public",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsVetted",
                schema: "public",
                table: "Users",
                column: "IsVetted");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                schema: "public",
                table: "Users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SceneName",
                schema: "public",
                table: "Users",
                column: "SceneName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "public",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_Email",
                table: "VettingApplications",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_Status",
                table: "VettingApplications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_SubmittedAt",
                table: "VettingApplications",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_UserId",
                table: "VettingApplications",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VettingAuditLogs_Action",
                table: "VettingAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_VettingAuditLogs_ApplicationId",
                table: "VettingAuditLogs",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingAuditLogs_PerformedAt",
                table: "VettingAuditLogs",
                column: "PerformedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VettingAuditLogs_PerformedBy",
                table: "VettingAuditLogs",
                column: "PerformedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperationItems_ApplicationId",
                schema: "public",
                table: "VettingBulkOperationItems",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperationItems_BulkOperationId",
                schema: "public",
                table: "VettingBulkOperationItems",
                column: "BulkOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperationItems_RetryAt",
                schema: "public",
                table: "VettingBulkOperationItems",
                column: "RetryAt",
                filter: "\"RetryAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperationItems_Success_ProcessedAt",
                schema: "public",
                table: "VettingBulkOperationItems",
                columns: new[] { "Success", "ProcessedAt" });

            migrationBuilder.CreateIndex(
                name: "UQ_VettingBulkOperationItems_Operation_Application",
                schema: "public",
                table: "VettingBulkOperationItems",
                columns: new[] { "BulkOperationId", "ApplicationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperationLogs_ApplicationId",
                schema: "public",
                table: "VettingBulkOperationLogs",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperationLogs_BulkOperationId",
                schema: "public",
                table: "VettingBulkOperationLogs",
                column: "BulkOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperationLogs_BulkOperationId_CreatedAt",
                schema: "public",
                table: "VettingBulkOperationLogs",
                columns: new[] { "BulkOperationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperationLogs_LogLevel",
                schema: "public",
                table: "VettingBulkOperationLogs",
                column: "LogLevel");

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperations_OperationType_PerformedAt",
                table: "VettingBulkOperations",
                columns: new[] { "OperationType", "PerformedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperations_Parameters",
                table: "VettingBulkOperations",
                column: "Parameters")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperations_PerformedBy_PerformedAt",
                table: "VettingBulkOperations",
                columns: new[] { "PerformedBy", "PerformedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VettingBulkOperations_Status",
                table: "VettingBulkOperations",
                column: "Status",
                filter: "\"Status\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_VettingEmailTemplates_IsActive",
                table: "VettingEmailTemplates",
                column: "IsActive",
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_VettingEmailTemplates_UpdatedAt",
                table: "VettingEmailTemplates",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VettingEmailTemplates_UpdatedBy",
                table: "VettingEmailTemplates",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UQ_VettingEmailTemplates_TemplateType",
                table: "VettingEmailTemplates",
                column: "TemplateType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VettingNotifications_ApplicationId",
                schema: "public",
                table: "VettingNotifications",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VettingNotifications_RecipientEmail",
                schema: "public",
                table: "VettingNotifications",
                column: "RecipientEmail");

            migrationBuilder.CreateIndex(
                name: "IX_VettingNotifications_Status",
                schema: "public",
                table: "VettingNotifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VettingNotifications_Status_CreatedAt",
                schema: "public",
                table: "VettingNotifications",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VettingNotifications_TemplateId",
                schema: "public",
                table: "VettingNotifications",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerPositions_EventId",
                schema: "public",
                table: "VolunteerPositions",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerPositions_SessionId",
                schema: "public",
                table: "VolunteerPositions",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheckInAuditLog",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CheckIns",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EventOrganizers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "IncidentAuditLog",
                schema: "public");

            migrationBuilder.DropTable(
                name: "IncidentNotifications",
                schema: "public");

            migrationBuilder.DropTable(
                name: "OfflineSyncQueue",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ParticipationHistory",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PaymentAuditLog",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PaymentFailures",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PaymentMethods",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PaymentRefunds",
                schema: "public");

            migrationBuilder.DropTable(
                name: "RoleClaims",
                schema: "public");

            migrationBuilder.DropTable(
                name: "TicketPurchases",
                schema: "public");

            migrationBuilder.DropTable(
                name: "UserClaims",
                schema: "public");

            migrationBuilder.DropTable(
                name: "UserLogins",
                schema: "public");

            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "public");

            migrationBuilder.DropTable(
                name: "UserTokens",
                schema: "public");

            migrationBuilder.DropTable(
                name: "VettingAuditLogs");

            migrationBuilder.DropTable(
                name: "VettingBulkOperationItems",
                schema: "public");

            migrationBuilder.DropTable(
                name: "VettingBulkOperationLogs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "VettingNotifications",
                schema: "public");

            migrationBuilder.DropTable(
                name: "VolunteerPositions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EventAttendees",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SafetyIncidents",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EventParticipations",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "public");

            migrationBuilder.DropTable(
                name: "TicketTypes",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "public");

            migrationBuilder.DropTable(
                name: "VettingBulkOperations");

            migrationBuilder.DropTable(
                name: "VettingApplications");

            migrationBuilder.DropTable(
                name: "VettingEmailTemplates");

            migrationBuilder.DropTable(
                name: "Sessions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Events",
                schema: "public");
        }
    }
}
