using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.Vetting.Entities.Configuration;

/// <summary>
/// Entity Framework configuration for VettingBulkOperation
/// Follows WitchCityRope patterns with PostgreSQL optimizations
/// </summary>
public class VettingBulkOperationConfiguration : IEntityTypeConfiguration<VettingBulkOperation>
{
    public void Configure(EntityTypeBuilder<VettingBulkOperation> builder)
    {
        // Table mapping
        builder.ToTable("VettingBulkOperations", "public");
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.OperationType)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(e => e.Status)
               .IsRequired()
               .HasConversion<int>()
               .HasDefaultValue(BulkOperationStatus.Running);

        // JSONB for flexible parameters - using PostgreSQL JSONB
        builder.Property(e => e.Parameters)
               .IsRequired()
               .HasColumnType("jsonb")
               .HasDefaultValue("{}");

        builder.Property(e => e.TotalItems)
               .IsRequired();

        builder.Property(e => e.SuccessCount)
               .IsRequired()
               .HasDefaultValue(0);

        builder.Property(e => e.FailureCount)
               .IsRequired()
               .HasDefaultValue(0);

        builder.Property(e => e.ErrorSummary)
               .HasColumnType("text");

        // DateTime properties - CRITICAL: Use timestamptz for PostgreSQL
        builder.Property(e => e.PerformedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.StartedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.CompletedAt)
               .HasColumnType("timestamptz");

        // Indexes
        builder.HasIndex(e => new { e.PerformedBy, e.PerformedAt })
               .HasDatabaseName("IX_VettingBulkOperations_PerformedBy_PerformedAt");

        builder.HasIndex(e => new { e.OperationType, e.PerformedAt })
               .HasDatabaseName("IX_VettingBulkOperations_OperationType_PerformedAt");

        builder.HasIndex(e => e.Status)
               .HasDatabaseName("IX_VettingBulkOperations_Status")
               .HasFilter("\"Status\" = 1"); // Running operations only

        // GIN index for JSONB parameters
        builder.HasIndex(e => e.Parameters)
               .HasDatabaseName("IX_VettingBulkOperations_Parameters")
               .HasMethod("gin");

        // Relationships
        builder.HasOne(e => e.PerformedByUser)
               .WithMany()
               .HasForeignKey(e => e.PerformedBy)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Items)
               .WithOne(i => i.BulkOperation)
               .HasForeignKey(i => i.BulkOperationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Logs)
               .WithOne(l => l.BulkOperation)
               .HasForeignKey(l => l.BulkOperationId)
               .OnDelete(DeleteBehavior.Cascade);

        // Check constraints
        builder.ToTable("VettingBulkOperations", t => t.HasCheckConstraint(
            "CHK_VettingBulkOperations_TotalItems",
            "\"TotalItems\" > 0"
        ));

        builder.ToTable("VettingBulkOperations", t => t.HasCheckConstraint(
            "CHK_VettingBulkOperations_Counts",
            "\"SuccessCount\" + \"FailureCount\" <= \"TotalItems\""
        ));
    }
}