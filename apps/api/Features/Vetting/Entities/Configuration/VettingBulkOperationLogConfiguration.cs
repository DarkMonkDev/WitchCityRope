using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.Vetting.Entities.Configuration;

/// <summary>
/// Entity Framework configuration for VettingBulkOperationLog
/// Follows WitchCityRope patterns with PostgreSQL optimizations
/// </summary>
public class VettingBulkOperationLogConfiguration : IEntityTypeConfiguration<VettingBulkOperationLog>
{
    public void Configure(EntityTypeBuilder<VettingBulkOperationLog> builder)
    {
        // Table mapping
        builder.ToTable("VettingBulkOperationLogs", "public");
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.LogLevel)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(e => e.Message)
               .IsRequired()
               .HasColumnType("text");

        // JSONB for flexible context - using PostgreSQL JSONB
        builder.Property(e => e.Context)
               .IsRequired()
               .HasColumnType("jsonb")
               .HasDefaultValue("{}");

        // DateTime properties - CRITICAL: Use timestamptz for PostgreSQL
        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        // Indexes
        builder.HasIndex(e => new { e.BulkOperationId, e.CreatedAt })
               .HasDatabaseName("IX_VettingBulkOperationLogs_BulkOperationId_CreatedAt");

        builder.HasIndex(e => e.LogLevel)
               .HasDatabaseName("IX_VettingBulkOperationLogs_LogLevel")
               .HasFilter("\"LogLevel\" IN ('Error', 'Critical')");

        // GIN index for JSONB context
        builder.HasIndex(e => e.Context)
               .HasDatabaseName("IX_VettingBulkOperationLogs_Context")
               .HasMethod("gin");

        // Relationships
        builder.HasOne(e => e.BulkOperation)
               .WithMany(bo => bo.Logs)
               .HasForeignKey(e => e.BulkOperationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Application)
               .WithMany()
               .HasForeignKey(e => e.ApplicationId)
               .OnDelete(DeleteBehavior.Cascade);

        // Check constraint for log levels
        builder.ToTable("VettingBulkOperationLogs", t => t.HasCheckConstraint(
            "CHK_VettingBulkOperationLogs_LogLevel",
            "\"LogLevel\" IN ('Debug', 'Info', 'Warning', 'Error', 'Critical')"
        ));
    }
}