using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.CheckIn.Entities.Configuration;

/// <summary>
/// Entity Framework configuration for OfflineSyncQueue entity
/// </summary>
public class OfflineSyncQueueConfiguration : IEntityTypeConfiguration<OfflineSyncQueue>
{
    public void Configure(EntityTypeBuilder<OfflineSyncQueue> builder)
    {
        // Table mapping with check constraints
        builder.ToTable("OfflineSyncQueue", "public", t =>
        {
            t.HasCheckConstraint("CHK_OfflineSyncQueue_ActionType",
                    "\"ActionType\" IN ('check-in', 'manual-entry', 'status-update', 'capacity-override')");

            t.HasCheckConstraint("CHK_OfflineSyncQueue_SyncStatus",
                    "\"SyncStatus\" IN ('pending', 'syncing', 'completed', 'failed', 'conflict')");

            t.HasCheckConstraint("CHK_OfflineSyncQueue_RetryCount",
                    "\"RetryCount\" >= 0 AND \"RetryCount\" <= 10");

            t.HasCheckConstraint("CHK_OfflineSyncQueue_SyncedAt",
                    "(\"SyncStatus\" = 'completed' AND \"SyncedAt\" IS NOT NULL) OR " +
                    "(\"SyncStatus\" != 'completed' AND \"SyncedAt\" IS NULL)");
        });

        builder.HasKey(o => o.Id);

        // Property configurations
        builder.Property(o => o.ActionType)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(o => o.ActionData)
               .IsRequired()
               .HasColumnType("jsonb")
               .HasDefaultValue("{}");

        builder.Property(o => o.LocalTimestamp)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(o => o.SyncStatus)
               .IsRequired()
               .HasMaxLength(20)
               .HasDefaultValue("pending");

        builder.Property(o => o.ErrorMessage)
               .HasColumnType("text");

        builder.Property(o => o.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(o => o.SyncedAt)
               .HasColumnType("timestamptz");

        // Relationships
        builder.HasOne(o => o.Event)
               .WithMany()
               .HasForeignKey(o => o.EventId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.User)
               .WithMany()
               .HasForeignKey(o => o.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(o => o.SyncStatus)
               .HasDatabaseName("IX_OfflineSyncQueue_SyncStatus");

        builder.HasIndex(o => o.EventId)
               .HasDatabaseName("IX_OfflineSyncQueue_EventId");

        builder.HasIndex(o => o.UserId)
               .HasDatabaseName("IX_OfflineSyncQueue_UserId");

        builder.HasIndex(o => o.LocalTimestamp)
               .HasDatabaseName("IX_OfflineSyncQueue_LocalTimestamp");

        builder.HasIndex(o => o.CreatedAt)
               .HasDatabaseName("IX_OfflineSyncQueue_Pending")
               .HasFilter("\"SyncStatus\" = 'pending'");

        builder.HasIndex(o => new { o.RetryCount, o.CreatedAt })
               .HasDatabaseName("IX_OfflineSyncQueue_Failed")
               .HasFilter("\"SyncStatus\" = 'failed' AND \"RetryCount\" < 5");

        // JSONB GIN index for action data
        builder.HasIndex(o => o.ActionData)
               .HasDatabaseName("IX_OfflineSyncQueue_ActionData")
               .HasMethod("gin");
    }
}