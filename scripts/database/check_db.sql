CREATE TABLE "Events" (
    "Id" uuid NOT NULL,
    "Title" character varying(200) NOT NULL,
    "Description" character varying(4000) NOT NULL,
    "StartDate" timestamp with time zone NOT NULL,
    "EndDate" timestamp with time zone NOT NULL,
    "Capacity" integer NOT NULL,
    "EventType" text NOT NULL,
    "Location" character varying(500) NOT NULL,
    "IsPublished" boolean NOT NULL DEFAULT FALSE,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "PricingTiers" TEXT NOT NULL,
    CONSTRAINT "PK_Events" PRIMARY KEY ("Id")
);


CREATE TABLE "Users" (
    "Id" uuid NOT NULL,
    "EncryptedLegalName" character varying(500) NOT NULL,
    "SceneName" character varying(100) NOT NULL,
    "Email" character varying(256) NOT NULL,
    "DateOfBirth" timestamp with time zone NOT NULL,
    "Role" text NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "PronouncedName" character varying(100) NOT NULL,
    "Pronouns" character varying(50) NOT NULL,
    "IsVetted" boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);


CREATE TABLE "EventOrganizers" (
    "EventId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    CONSTRAINT "PK_EventOrganizers" PRIMARY KEY ("EventId", "UserId"),
    CONSTRAINT "FK_EventOrganizers_Events_EventId" FOREIGN KEY ("EventId") REFERENCES "Events" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_EventOrganizers_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);


CREATE TABLE "IncidentReports" (
    "Id" uuid NOT NULL,
    "ReporterId" uuid,
    "EventId" uuid,
    "Description" character varying(4000) NOT NULL,
    "Severity" text NOT NULL,
    "IsAnonymous" boolean NOT NULL,
    "Status" text NOT NULL,
    "ReportedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "ResolvedAt" timestamp with time zone,
    "ResolutionNotes" character varying(2000) NOT NULL,
    "ReferenceNumber" character varying(50) NOT NULL,
    "IncidentType" text NOT NULL,
    "Location" character varying(500) NOT NULL,
    "IncidentDate" timestamp with time zone NOT NULL,
    "RequestFollowUp" boolean NOT NULL,
    "PreferredContactMethod" character varying(100) NOT NULL,
    "AssignedToId" uuid,
    CONSTRAINT "PK_IncidentReports" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_IncidentReports_Events_EventId" FOREIGN KEY ("EventId") REFERENCES "Events" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_IncidentReports_Users_AssignedToId" FOREIGN KEY ("AssignedToId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_IncidentReports_Users_ReporterId" FOREIGN KEY ("ReporterId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "RefreshTokens" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Token" character varying(512) NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "IsRevoked" boolean NOT NULL,
    "RevokedAt" timestamp with time zone,
    "ReplacedByToken" character varying(512),
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Registrations" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "EventId" uuid NOT NULL,
    "SelectedPriceAmount" numeric(18,2) NOT NULL,
    "SelectedPriceCurrency" character varying(3) NOT NULL,
    "Status" text NOT NULL,
    "DietaryRestrictions" character varying(500) NOT NULL,
    "AccessibilityNeeds" character varying(500) NOT NULL,
    "EmergencyContactName" character varying(200) NOT NULL,
    "EmergencyContactPhone" character varying(50) NOT NULL,
    "RegisteredAt" timestamp with time zone NOT NULL,
    "ConfirmedAt" timestamp with time zone,
    "CancelledAt" timestamp with time zone,
    "CancellationReason" character varying(500) NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "CheckedInAt" timestamp with time zone,
    "CheckedInBy" uuid,
    "CreatedAt" timestamp with time zone NOT NULL,
    "ConfirmationCode" character varying(50) NOT NULL,
    CONSTRAINT "PK_Registrations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Registrations_Events_EventId" FOREIGN KEY ("EventId") REFERENCES "Events" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Registrations_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "UserAuthentications" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "PasswordHash" character varying(256) NOT NULL,
    "TwoFactorSecret" character varying(256),
    "IsTwoFactorEnabled" boolean NOT NULL,
    "LastPasswordChangeAt" timestamp with time zone,
    "FailedLoginAttempts" integer NOT NULL,
    "LockedOutUntil" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_UserAuthentications" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserAuthentications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);


CREATE TABLE "VettingApplications" (
    "Id" uuid NOT NULL,
    "ApplicantId" uuid NOT NULL,
    "ExperienceLevel" character varying(500) NOT NULL,
    "Interests" character varying(1000) NOT NULL,
    "SafetyKnowledge" character varying(2000) NOT NULL,
    "ExperienceDescription" character varying(2000) NOT NULL,
    "ConsentUnderstanding" character varying(2000) NOT NULL,
    "WhyJoin" character varying(2000) NOT NULL,
    "Status" text NOT NULL,
    "SubmittedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "ReviewedAt" timestamp with time zone,
    "DecisionNotes" character varying(1000),
    "References" TEXT NOT NULL,
    CONSTRAINT "PK_VettingApplications" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_VettingApplications_Users_ApplicantId" FOREIGN KEY ("ApplicantId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "IncidentActions" (
    "Id" uuid NOT NULL,
    "ActionType" character varying(100) NOT NULL,
    "Description" character varying(1000) NOT NULL,
    "PerformedById" uuid NOT NULL,
    "PerformedAt" timestamp with time zone NOT NULL,
    "IncidentReportId" uuid NOT NULL,
    CONSTRAINT "PK_IncidentActions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_IncidentActions_IncidentReports_IncidentReportId" FOREIGN KEY ("IncidentReportId") REFERENCES "IncidentReports" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_IncidentActions_Users_PerformedById" FOREIGN KEY ("PerformedById") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "IncidentReviews" (
    "Id" uuid NOT NULL,
    "ReviewerId" uuid NOT NULL,
    "Findings" character varying(2000) NOT NULL,
    "RecommendedSeverity" text NOT NULL,
    "RecommendAction" boolean NOT NULL,
    "ReviewedAt" timestamp with time zone NOT NULL,
    "IncidentReportId" uuid NOT NULL,
    CONSTRAINT "PK_IncidentReviews" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_IncidentReviews_IncidentReports_IncidentReportId" FOREIGN KEY ("IncidentReportId") REFERENCES "IncidentReports" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_IncidentReviews_Users_ReviewerId" FOREIGN KEY ("ReviewerId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "Payments" (
    "Id" uuid NOT NULL,
    "RegistrationId" uuid NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "Currency" character varying(3) NOT NULL,
    "Status" text NOT NULL,
    "PaymentMethod" character varying(50) NOT NULL,
    "TransactionId" character varying(100) NOT NULL,
    "ProcessedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "RefundAmount" numeric(18,2) NOT NULL,
    "RefundCurrency" character varying(3) NOT NULL,
    "RefundedAt" timestamp with time zone,
    "RefundTransactionId" character varying(100) NOT NULL,
    "RefundReason" character varying(500) NOT NULL,
    CONSTRAINT "PK_Payments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Payments_Registrations_RegistrationId" FOREIGN KEY ("RegistrationId") REFERENCES "Registrations" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "VettingReviews" (
    "Id" uuid NOT NULL,
    "ReviewerId" uuid NOT NULL,
    "Recommendation" boolean NOT NULL,
    "Notes" character varying(1000) NOT NULL,
    "ReviewedAt" timestamp with time zone NOT NULL,
    "VettingApplicationId" uuid NOT NULL,
    CONSTRAINT "PK_VettingReviews" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_VettingReviews_Users_ReviewerId" FOREIGN KEY ("ReviewerId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_VettingReviews_VettingApplications_VettingApplicationId" FOREIGN KEY ("VettingApplicationId") REFERENCES "VettingApplications" ("Id") ON DELETE CASCADE
);


CREATE INDEX "IX_EventOrganizers_UserId" ON "EventOrganizers" ("UserId");


CREATE INDEX "IX_Events_EventType" ON "Events" ("EventType");


CREATE INDEX "IX_Events_IsPublished" ON "Events" ("IsPublished");


CREATE INDEX "IX_Events_IsPublished_StartDate" ON "Events" ("IsPublished", "StartDate");


CREATE INDEX "IX_Events_StartDate" ON "Events" ("StartDate");


CREATE INDEX "IX_IncidentActions_IncidentReportId" ON "IncidentActions" ("IncidentReportId");


CREATE INDEX "IX_IncidentActions_PerformedById" ON "IncidentActions" ("PerformedById");


CREATE INDEX "IX_IncidentReports_AssignedToId" ON "IncidentReports" ("AssignedToId");


CREATE INDEX "IX_IncidentReports_EventId" ON "IncidentReports" ("EventId");


CREATE INDEX "IX_IncidentReports_IncidentDate" ON "IncidentReports" ("IncidentDate");


CREATE INDEX "IX_IncidentReports_IncidentType" ON "IncidentReports" ("IncidentType");


CREATE INDEX "IX_IncidentReports_IsAnonymous" ON "IncidentReports" ("IsAnonymous");


CREATE UNIQUE INDEX "IX_IncidentReports_ReferenceNumber" ON "IncidentReports" ("ReferenceNumber");


CREATE INDEX "IX_IncidentReports_ReportedAt" ON "IncidentReports" ("ReportedAt");


CREATE INDEX "IX_IncidentReports_ReporterId" ON "IncidentReports" ("ReporterId");


CREATE INDEX "IX_IncidentReports_Severity" ON "IncidentReports" ("Severity");


CREATE INDEX "IX_IncidentReports_Status" ON "IncidentReports" ("Status");


CREATE INDEX "IX_IncidentReviews_IncidentReportId" ON "IncidentReviews" ("IncidentReportId");


CREATE INDEX "IX_IncidentReviews_ReviewerId" ON "IncidentReviews" ("ReviewerId");


CREATE INDEX "IX_Payments_ProcessedAt" ON "Payments" ("ProcessedAt");


CREATE INDEX "IX_Payments_RefundTransactionId" ON "Payments" ("RefundTransactionId");


CREATE UNIQUE INDEX "IX_Payments_RegistrationId" ON "Payments" ("RegistrationId");


CREATE INDEX "IX_Payments_Status" ON "Payments" ("Status");


CREATE UNIQUE INDEX "IX_Payments_TransactionId" ON "Payments" ("TransactionId");


CREATE INDEX "IX_RefreshTokens_ExpiresAt" ON "RefreshTokens" ("ExpiresAt");


CREATE UNIQUE INDEX "IX_RefreshTokens_Token" ON "RefreshTokens" ("Token");


CREATE INDEX "IX_RefreshTokens_UserId" ON "RefreshTokens" ("UserId");


CREATE INDEX "IX_Registrations_CheckedInAt" ON "Registrations" ("CheckedInAt");


CREATE UNIQUE INDEX "IX_Registrations_ConfirmationCode" ON "Registrations" ("ConfirmationCode");


CREATE INDEX "IX_Registrations_EventId" ON "Registrations" ("EventId");


CREATE INDEX "IX_Registrations_RegisteredAt" ON "Registrations" ("RegisteredAt");


CREATE INDEX "IX_Registrations_Status" ON "Registrations" ("Status");


CREATE INDEX "IX_Registrations_UserId" ON "Registrations" ("UserId");


CREATE UNIQUE INDEX "IX_Registrations_UserId_EventId" ON "Registrations" ("UserId", "EventId");


CREATE UNIQUE INDEX "IX_UserAuthentications_UserId" ON "UserAuthentications" ("UserId");


CREATE INDEX "IX_Users_CreatedAt" ON "Users" ("CreatedAt");


CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");


CREATE INDEX "IX_Users_IsActive" ON "Users" ("IsActive");


CREATE INDEX "IX_Users_IsVetted" ON "Users" ("IsVetted");


CREATE UNIQUE INDEX "IX_Users_SceneName" ON "Users" ("SceneName");


CREATE INDEX "IX_VettingApplications_ApplicantId" ON "VettingApplications" ("ApplicantId");


CREATE INDEX "IX_VettingApplications_Status" ON "VettingApplications" ("Status");


CREATE INDEX "IX_VettingApplications_SubmittedAt" ON "VettingApplications" ("SubmittedAt");


CREATE INDEX "IX_VettingReviews_ReviewerId" ON "VettingReviews" ("ReviewerId");


CREATE INDEX "IX_VettingReviews_VettingApplicationId" ON "VettingReviews" ("VettingApplicationId");


