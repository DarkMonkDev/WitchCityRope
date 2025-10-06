using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorVettingStatusEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Map old enum values to new enum values
            // Old: Draft=0, Submitted=1, UnderReview=2, InterviewApproved=3, PendingInterview=4, InterviewScheduled=5, OnHold=6, Approved=7, Denied=8, Withdrawn=9
            // New: UnderReview=0, InterviewApproved=1, InterviewScheduled=2, FinalReview=3, Approved=4, Denied=5, OnHold=6, Withdrawn=7

            migrationBuilder.Sql(@"
                -- Update VettingApplications Status values
                UPDATE ""VettingApplications""
                SET ""Status"" = CASE ""Status""
                    WHEN 0 THEN 0  -- Draft → UnderReview
                    WHEN 1 THEN 0  -- Submitted → UnderReview
                    WHEN 2 THEN 0  -- UnderReview → UnderReview
                    WHEN 3 THEN 1  -- InterviewApproved → InterviewApproved
                    WHEN 4 THEN 1  -- PendingInterview → InterviewApproved
                    WHEN 5 THEN 2  -- InterviewScheduled → InterviewScheduled
                    WHEN 6 THEN 6  -- OnHold → OnHold
                    WHEN 7 THEN 4  -- Approved → Approved
                    WHEN 8 THEN 5  -- Denied → Denied
                    WHEN 9 THEN 7  -- Withdrawn → Withdrawn
                    ELSE ""Status""
                END;

                -- Update VettingAuditLogs OldValue and NewValue strings
                UPDATE ""VettingAuditLogs""
                SET ""OldValue"" = CASE ""OldValue""
                    WHEN 'Draft' THEN 'UnderReview'
                    WHEN 'Submitted' THEN 'UnderReview'
                    WHEN 'PendingInterview' THEN 'InterviewApproved'
                    ELSE ""OldValue""
                END,
                ""NewValue"" = CASE ""NewValue""
                    WHEN 'Draft' THEN 'UnderReview'
                    WHEN 'Submitted' THEN 'UnderReview'
                    WHEN 'PendingInterview' THEN 'InterviewApproved'
                    ELSE ""NewValue""
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback: Map new values back to old values
            migrationBuilder.Sql(@"
                -- Restore VettingApplications Status values
                UPDATE ""VettingApplications""
                SET ""Status"" = CASE ""Status""
                    WHEN 0 THEN 2  -- UnderReview → UnderReview (old value)
                    WHEN 1 THEN 3  -- InterviewApproved → InterviewApproved (old value)
                    WHEN 2 THEN 5  -- InterviewScheduled → InterviewScheduled (old value)
                    WHEN 3 THEN 2  -- FinalReview → UnderReview (closest match)
                    WHEN 4 THEN 7  -- Approved → Approved (old value)
                    WHEN 5 THEN 8  -- Denied → Denied (old value)
                    WHEN 6 THEN 6  -- OnHold → OnHold (same)
                    WHEN 7 THEN 9  -- Withdrawn → Withdrawn (old value)
                    ELSE ""Status""
                END;

                -- Restore VettingAuditLogs strings (best effort)
                UPDATE ""VettingAuditLogs""
                SET ""OldValue"" = CASE ""OldValue""
                    WHEN 'UnderReview' THEN 'Submitted'
                    ELSE ""OldValue""
                END,
                ""NewValue"" = CASE ""NewValue""
                    WHEN 'UnderReview' THEN 'Submitted'
                    ELSE ""NewValue""
                END;
            ");
        }
    }
}
