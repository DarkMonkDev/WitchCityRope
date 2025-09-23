using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.Vetting.Entities.Configuration;

/// <summary>
/// Entity Framework configuration for VettingBulkOperationLog
/// </summary>
public class VettingBulkOperationLogConfiguration : IEntityTypeConfiguration<VettingBulkOperationLog>
{
    public void Configure(EntityTypeBuilder<VettingBulkOperationLog> builder)
    {
        builder.ToTable("VettingBulkOperationLogs", "public");
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.LogLevel)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.Details)
            .HasColumnType("text");

        builder.Property(e => e.OperationStep)
            .HasMaxLength(100);

        builder.Property(e => e.ErrorCode)
            .HasMaxLength(50);

        builder.Property(e => e.StackTrace)
            .HasColumnType("text");

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        // Relationships
        builder.HasOne(e => e.BulkOperation)
            .WithMany(b => b.Logs)
            .HasForeignKey(e => e.BulkOperationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Application)
            .WithMany()
            .HasForeignKey(e => e.ApplicationId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(e => e.BulkOperationId)
            .HasDatabaseName("IX_VettingBulkOperationLogs_BulkOperationId");

        builder.HasIndex(e => new { e.BulkOperationId, e.CreatedAt })
            .HasDatabaseName("IX_VettingBulkOperationLogs_BulkOperationId_CreatedAt");

        builder.HasIndex(e => e.LogLevel)
            .HasDatabaseName("IX_VettingBulkOperationLogs_LogLevel");
    }
}