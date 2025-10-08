using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.CheckIn.Entities.Configuration;

/// <summary>
/// Entity Framework configuration for CheckInAuditLog entity
/// </summary>
public class CheckInAuditLogConfiguration : IEntityTypeConfiguration<CheckInAuditLog>
{
    public void Configure(EntityTypeBuilder<CheckInAuditLog> builder)
    {
        // Table mapping with check constraints
        builder.ToTable("CheckInAuditLog", "public", t =>
        {
            t.HasCheckConstraint("CHK_CheckInAuditLog_ActionType",
                    "\"ActionType\" IN ('check-in', 'manual-entry', 'capacity-override', 'status-change', 'data-update')");
        });

        builder.HasKey(a => a.Id);

        // Property configurations
        builder.Property(a => a.ActionType)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(a => a.ActionDescription)
               .IsRequired()
               .HasColumnType("text");

        builder.Property(a => a.OldValues)
               .HasColumnType("jsonb");

        builder.Property(a => a.NewValues)
               .HasColumnType("jsonb");

        builder.Property(a => a.IpAddress)
               .HasMaxLength(45);

        builder.Property(a => a.UserAgent)
               .HasMaxLength(500);

        builder.Property(a => a.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        // Relationships
        builder.HasOne(a => a.EventAttendee)
               .WithMany(e => e.AuditLogs)
               .HasForeignKey(a => a.EventAttendeeId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.Event)
               .WithMany()
               .HasForeignKey(a => a.EventId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.CreatedByUser)
               .WithMany()
               .HasForeignKey(a => a.CreatedBy)
               .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(a => a.EventId)
               .HasDatabaseName("IX_CheckInAuditLog_EventId");

        builder.HasIndex(a => a.EventAttendeeId)
               .HasDatabaseName("IX_CheckInAuditLog_EventAttendeeId");

        builder.HasIndex(a => a.ActionType)
               .HasDatabaseName("IX_CheckInAuditLog_ActionType");

        builder.HasIndex(a => a.CreatedAt)
               .HasDatabaseName("IX_CheckInAuditLog_CreatedAt");

        builder.HasIndex(a => new { a.EventId, a.CreatedAt })
               .HasDatabaseName("IX_CheckInAuditLog_Event_Time");

        builder.HasIndex(a => new { a.CreatedBy, a.CreatedAt })
               .HasDatabaseName("IX_CheckInAuditLog_User_Time");

        // JSONB GIN indexes
        builder.HasIndex(a => a.OldValues)
               .HasDatabaseName("IX_CheckInAuditLog_OldValues")
               .HasMethod("gin")
               .HasFilter("\"OldValues\" IS NOT NULL");

        builder.HasIndex(a => a.NewValues)
               .HasDatabaseName("IX_CheckInAuditLog_NewValues")
               .HasMethod("gin")
               .HasFilter("\"NewValues\" IS NOT NULL");
    }
}