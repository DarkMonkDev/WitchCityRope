using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Additional indexes for Users table
            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                table: "Users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role_IsActive",
                table: "Users",
                columns: new[] { "Role", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_UpdatedAt",
                table: "Users",
                column: "UpdatedAt");

            // Additional indexes for Events table
            migrationBuilder.CreateIndex(
                name: "IX_Events_EndDate",
                table: "Events",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_Events_CreatedAt",
                table: "Events",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Events_UpdatedAt",
                table: "Events",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Location",
                table: "Events",
                column: "Location");

            // Composite index for event availability queries
            migrationBuilder.CreateIndex(
                name: "IX_Events_IsPublished_EventType_StartDate",
                table: "Events",
                columns: new[] { "IsPublished", "EventType", "StartDate" });

            // Additional indexes for Registrations table
            migrationBuilder.CreateIndex(
                name: "IX_Registrations_ConfirmedAt",
                table: "Registrations",
                column: "ConfirmedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_CancelledAt",
                table: "Registrations",
                column: "CancelledAt");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_UpdatedAt",
                table: "Registrations",
                column: "UpdatedAt");

            // Composite index for user registration queries
            migrationBuilder.CreateIndex(
                name: "IX_Registrations_UserId_Status",
                table: "Registrations",
                columns: new[] { "UserId", "Status" });

            // Composite index for event registration queries
            migrationBuilder.CreateIndex(
                name: "IX_Registrations_EventId_Status",
                table: "Registrations",
                columns: new[] { "EventId", "Status" });

            // Composite index for event registration counts
            migrationBuilder.CreateIndex(
                name: "IX_Registrations_EventId_Status_RegisteredAt",
                table: "Registrations",
                columns: new[] { "EventId", "Status", "RegisteredAt" });

            // Additional indexes for Payments table
            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentMethod",
                table: "Payments",
                column: "PaymentMethod");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UpdatedAt",
                table: "Payments",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RefundedAt",
                table: "Payments",
                column: "RefundedAt");

            // Composite index for payment status queries
            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status_ProcessedAt",
                table: "Payments",
                columns: new[] { "Status", "ProcessedAt" });

            // Composite index for payment reconciliation
            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentMethod_Status_ProcessedAt",
                table: "Payments",
                columns: new[] { "PaymentMethod", "Status", "ProcessedAt" });

            // Additional indexes for VettingApplications table
            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_ReviewedAt",
                table: "VettingApplications",
                column: "ReviewedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_UpdatedAt",
                table: "VettingApplications",
                column: "UpdatedAt");

            // Composite index for vetting workflow queries
            migrationBuilder.CreateIndex(
                name: "IX_VettingApplications_Status_SubmittedAt",
                table: "VettingApplications",
                columns: new[] { "Status", "SubmittedAt" });

            // Additional indexes for IncidentReports table
            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_UpdatedAt",
                table: "IncidentReports",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_ResolvedAt",
                table: "IncidentReports",
                column: "ResolvedAt");

            // Composite index for incident report queries
            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_Status_Severity_ReportedAt",
                table: "IncidentReports",
                columns: new[] { "Status", "Severity", "ReportedAt" });

            // Composite index for event incident queries
            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_EventId_Status",
                table: "IncidentReports",
                columns: new[] { "EventId", "Status" });

            // Additional indexes for IncidentActions table
            migrationBuilder.CreateIndex(
                name: "IX_IncidentActions_PerformedAt",
                table: "IncidentActions",
                column: "PerformedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentActions_ActionType",
                table: "IncidentActions",
                column: "ActionType");

            // Composite index for incident action audit trail
            migrationBuilder.CreateIndex(
                name: "IX_IncidentActions_IncidentReportId_PerformedAt",
                table: "IncidentActions",
                columns: new[] { "IncidentReportId", "PerformedAt" });

            // Additional indexes for IncidentReviews table
            migrationBuilder.CreateIndex(
                name: "IX_IncidentReviews_ReviewedAt",
                table: "IncidentReviews",
                column: "ReviewedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReviews_RecommendedSeverity",
                table: "IncidentReviews",
                column: "RecommendedSeverity");

            // Additional indexes for VettingReviews table
            migrationBuilder.CreateIndex(
                name: "IX_VettingReviews_ReviewedAt",
                table: "VettingReviews",
                column: "ReviewedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VettingReviews_Recommendation",
                table: "VettingReviews",
                column: "Recommendation");

            // Composite index for vetting review queries
            migrationBuilder.CreateIndex(
                name: "IX_VettingReviews_VettingApplicationId_ReviewedAt",
                table: "VettingReviews",
                columns: new[] { "VettingApplicationId", "ReviewedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes in reverse order
            migrationBuilder.DropIndex(
                name: "IX_VettingReviews_VettingApplicationId_ReviewedAt",
                table: "VettingReviews");

            migrationBuilder.DropIndex(
                name: "IX_VettingReviews_Recommendation",
                table: "VettingReviews");

            migrationBuilder.DropIndex(
                name: "IX_VettingReviews_ReviewedAt",
                table: "VettingReviews");

            migrationBuilder.DropIndex(
                name: "IX_IncidentReviews_RecommendedSeverity",
                table: "IncidentReviews");

            migrationBuilder.DropIndex(
                name: "IX_IncidentReviews_ReviewedAt",
                table: "IncidentReviews");

            migrationBuilder.DropIndex(
                name: "IX_IncidentActions_IncidentReportId_PerformedAt",
                table: "IncidentActions");

            migrationBuilder.DropIndex(
                name: "IX_IncidentActions_ActionType",
                table: "IncidentActions");

            migrationBuilder.DropIndex(
                name: "IX_IncidentActions_PerformedAt",
                table: "IncidentActions");

            migrationBuilder.DropIndex(
                name: "IX_IncidentReports_EventId_Status",
                table: "IncidentReports");

            migrationBuilder.DropIndex(
                name: "IX_IncidentReports_Status_Severity_ReportedAt",
                table: "IncidentReports");

            migrationBuilder.DropIndex(
                name: "IX_IncidentReports_ResolvedAt",
                table: "IncidentReports");

            migrationBuilder.DropIndex(
                name: "IX_IncidentReports_UpdatedAt",
                table: "IncidentReports");

            migrationBuilder.DropIndex(
                name: "IX_VettingApplications_Status_SubmittedAt",
                table: "VettingApplications");

            migrationBuilder.DropIndex(
                name: "IX_VettingApplications_UpdatedAt",
                table: "VettingApplications");

            migrationBuilder.DropIndex(
                name: "IX_VettingApplications_ReviewedAt",
                table: "VettingApplications");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentMethod_Status_ProcessedAt",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Status_ProcessedAt",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_RefundedAt",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_UpdatedAt",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentMethod",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_EventId_Status_RegisteredAt",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_EventId_Status",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_UserId_Status",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_UpdatedAt",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_CancelledAt",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_ConfirmedAt",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Events_IsPublished_EventType_StartDate",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_Location",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_UpdatedAt",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_CreatedAt",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_EndDate",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Users_UpdatedAt",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Role_IsActive",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Role",
                table: "Users");
        }
    }
}