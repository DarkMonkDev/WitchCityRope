using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSafetyReferenceNumberFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create PostgreSQL function to generate unique safety reference numbers
            // Format: SR-YYYY-NNNNNN (Safety Report - Year - Sequential Number)
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION generate_safety_reference_number()
                RETURNS TEXT
                LANGUAGE plpgsql
                AS $$
                DECLARE
                    year_part TEXT;
                    sequence_num INTEGER;
                    sequence_part TEXT;
                    reference TEXT;
                BEGIN
                    -- Get current year
                    year_part := EXTRACT(YEAR FROM CURRENT_TIMESTAMP)::TEXT;

                    -- Get next sequence number for this year
                    -- Count existing incidents for current year + 1
                    SELECT COUNT(*) + 1 INTO sequence_num
                    FROM public.""SafetyIncidents""
                    WHERE EXTRACT(YEAR FROM ""ReportedAt"") = EXTRACT(YEAR FROM CURRENT_TIMESTAMP);

                    -- Pad sequence number to 6 digits
                    sequence_part := LPAD(sequence_num::TEXT, 6, '0');

                    -- Format: SR-YYYY-NNNNNN
                    reference := 'SR-' || year_part || '-' || sequence_part;

                    RETURN reference;
                END;
                $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the PostgreSQL function
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS generate_safety_reference_number();");
        }
    }
}
