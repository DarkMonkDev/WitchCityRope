using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.Vetting.Entities.Configuration;

/// <summary>
/// Entity Framework configuration for VettingBulkOperationItem
/// Follows WitchCityRope patterns with PostgreSQL optimizations
/// </summary>
public class VettingBulkOperationItemConfiguration : IEntityTypeConfiguration<VettingBulkOperationItem>
{
    public void Configure(EntityTypeBuilder<VettingBulkOperationItem> builder)
    {
        // Table mapping
        builder.ToTable("VettingBulkOperationItems", "public");
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.Success)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(e => e.ErrorMessage)
               .HasColumnType("text");

        builder.Property(e => e.AttemptCount)
               .IsRequired()
               .HasDefaultValue(1);

        // DateTime properties - CRITICAL: Use timestamptz for PostgreSQL
        builder.Property(e => e.ProcessedAt)
               .HasColumnType("timestamptz");

        builder.Property(e => e.RetryAt)
               .HasColumnType("timestamptz");

        // Indexes
        builder.HasIndex(e => e.BulkOperationId)
               .HasDatabaseName("IX_VettingBulkOperationItems_BulkOperationId");

        builder.HasIndex(e => e.ApplicationId)
               .HasDatabaseName("IX_VettingBulkOperationItems_ApplicationId");

        builder.HasIndex(e => new { e.Success, e.ProcessedAt })
               .HasDatabaseName("IX_VettingBulkOperationItems_Success_ProcessedAt");

        builder.HasIndex(e => e.RetryAt)
               .HasDatabaseName("IX_VettingBulkOperationItems_RetryAt")
               .HasFilter("\"RetryAt\" IS NOT NULL");

        // Relationships
        builder.HasOne(e => e.BulkOperation)
               .WithMany(bo => bo.Items)
               .HasForeignKey(e => e.BulkOperationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Application)
               .WithMany()
               .HasForeignKey(e => e.ApplicationId)
               .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint for operation + application combination
        builder.HasIndex(e => new { e.BulkOperationId, e.ApplicationId })
               .IsUnique()
               .HasDatabaseName("UQ_VettingBulkOperationItems_Operation_Application");
    }
}