using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <summary>
    /// Phase 3b-2 of the vetting status cleanup: change ApplicationUser.VettingStatus
    /// from int to the VettingStatus enum. No schema change at the column level
    /// because EF Core stores enums as their underlying int type by default — the
    /// database column remains `integer` and all existing rows are preserved.
    ///
    /// This migration adds a CHECK constraint defending against invalid enum
    /// values being written to the VettingStatus column. Valid values are 0–6
    /// corresponding to UnderReview, InterviewApproved, FinalReview, Approved,
    /// Denied, OnHold, and Withdrawn (see
    /// apps/api/Features/Vetting/Entities/VettingApplication.cs). A code-review
    /// finding during Phase 3b-1 noted that `(VettingStatus)99` is legal C# and
    /// would silently propagate through to the DB — the constraint below closes
    /// that gap at the database layer.
    ///
    /// Reversible: dropping the constraint in Down() returns the column to its
    /// pre-migration permissive state.
    /// </summary>
    public partial class Phase3b2VettingStatusEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Note: ASP.NET Identity is customized to use the "Users" table
            // (rather than the default "AspNetUsers") via the entity's
            // ToTable("Users") mapping in ApplicationDbContext. See the
            // existing Data/Migrations/*.Designer.cs files for confirmation.
            //
            // Table is schema-qualified as public."Users" to match the
            // convention used throughout the rest of the project's
            // auto-generated migrations and to protect against search_path
            // surprises on staging/production.
            migrationBuilder.Sql(@"
                ALTER TABLE public.""Users""
                ADD CONSTRAINT ""CK_Users_VettingStatus_Range""
                CHECK (""VettingStatus"" BETWEEN 0 AND 6);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE public.""Users""
                DROP CONSTRAINT IF EXISTS ""CK_Users_VettingStatus_Range"";
            ");
        }
    }
}
