using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Api.Features.Logging.Entities;

namespace WitchCityRope.Api.Features.Logging.Configuration;

/// <summary>
/// EF Core configuration for DailyLogSummary entity.
/// Written by Hangfire aggregation jobs, queried by admin reporting endpoints.
/// </summary>
public class DailyLogSummaryConfiguration : IEntityTypeConfiguration<DailyLogSummary>
{
    public void Configure(EntityTypeBuilder<DailyLogSummary> builder)
    {
        builder.ToTable("daily_log_summaries", "logging");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
               .UseIdentityAlwaysColumn();

        builder.Property(s => s.Date)
               .HasColumnType("date")
               .IsRequired();

        builder.Property(s => s.Category)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(s => s.Subcategory)
               .HasMaxLength(200);

        builder.Property(s => s.Count)
               .IsRequired();

        builder.Property(s => s.Metadata)
               .HasColumnType("jsonb");

        builder.Property(s => s.CreatedAt)
               .HasColumnType("timestamptz")
               .HasDefaultValueSql("NOW()")
               .IsRequired();

        // Composite index for date + category lookups
        builder.HasIndex(s => new { s.Date, s.Category })
               .HasDatabaseName("IX_daily_log_summaries_date_category");

        // Index on category for category-based queries
        builder.HasIndex(s => s.Category)
               .HasDatabaseName("IX_daily_log_summaries_category");

        // Unique constraint for upsert pattern (one row per date/category/subcategory)
        builder.HasIndex(s => new { s.Date, s.Category, s.Subcategory })
               .HasDatabaseName("UQ_daily_log_summaries_date_category_subcategory")
               .IsUnique();
    }
}
