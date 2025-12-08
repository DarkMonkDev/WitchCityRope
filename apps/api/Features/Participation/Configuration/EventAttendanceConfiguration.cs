using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Api.Features.Participation.Entities;

namespace WitchCityRope.Api.Features.Participation.Configuration;

/// <summary>
/// Entity Framework configuration for EventAttendance entity
/// Implements PostgreSQL-specific patterns and constraints
/// </summary>
public class EventAttendanceConfiguration : IEntityTypeConfiguration<EventAttendance>
{
    public void Configure(EntityTypeBuilder<EventAttendance> builder)
    {
        // Table mapping
        builder.ToTable("EventAttendances", "public");
        builder.HasKey(e => e.Id);

        // Property configurations with PostgreSQL patterns
        builder.Property(e => e.Id)
               .ValueGeneratedOnAdd(); // Let PostgreSQL generate UUIDs

        builder.Property(e => e.EventId)
               .IsRequired();

        builder.Property(e => e.UserId)
               .IsRequired();

        builder.Property(e => e.AttendanceType)
               .IsRequired()
               .HasConversion<int>(); // Store as INTEGER

        builder.Property(e => e.Status)
               .IsRequired()
               .HasConversion<int>(); // Store as INTEGER

        builder.Property(e => e.TicketPurchaseId)
               .IsRequired(false); // Nullable

        // CRITICAL: UTC DateTime handling for PostgreSQL
        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.CancelledAt)
               .HasColumnType("timestamptz");

        builder.Property(e => e.UpdatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.CancellationReason)
               .HasMaxLength(1000);

        builder.Property(e => e.Notes)
               .HasMaxLength(2000);

        // JSONB configuration for PostgreSQL
        builder.Property(e => e.Metadata)
               .IsRequired()
               .HasColumnType("jsonb")
               .HasDefaultValue("{}");

        // Event Waiver fields for legal compliance
        builder.Property(e => e.EventWaiverAccepted)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(e => e.EventWaiverAcceptedAt)
               .HasColumnType("timestamptz");

        // Foreign key relationships
        builder.HasOne(e => e.Event)
               .WithMany(e => e.EventAttendances)
               .HasForeignKey(e => e.EventId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
               .WithMany()
               .HasForeignKey(e => e.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.CreatedByUser)
               .WithMany()
               .HasForeignKey(e => e.CreatedBy)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.UpdatedByUser)
               .WithMany()
               .HasForeignKey(e => e.UpdatedBy)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.TicketPurchase)
               .WithMany(tp => tp.EventAttendances)
               .HasForeignKey(e => e.TicketPurchaseId)
               .OnDelete(DeleteBehavior.Cascade);

        // Session relationship (nullable for backward compatibility)
        builder.HasOne(e => e.Session)
               .WithMany()
               .HasForeignKey(e => e.SessionId)
               .OnDelete(DeleteBehavior.Cascade)
               .IsRequired(false);

        // One-to-many relationship with AttendanceHistory
        builder.HasMany(e => e.History)
               .WithOne(h => h.Attendance)
               .HasForeignKey(h => h.AttendanceId)
               .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(e => new { e.EventId, e.Status })
               .HasDatabaseName("IX_EventAttendances_EventId_Status");

        builder.HasIndex(e => new { e.UserId, e.Status })
               .HasDatabaseName("IX_EventAttendances_UserId_Status");

        builder.HasIndex(e => e.CreatedAt)
               .HasDatabaseName("IX_EventAttendances_CreatedAt");

        builder.HasIndex(e => e.TicketPurchaseId)
               .HasDatabaseName("IX_EventAttendances_TicketPurchaseId");

        // Session indexes for performance
        builder.HasIndex(e => e.SessionId)
               .HasDatabaseName("IX_EventAttendances_SessionId");

        builder.HasIndex(e => new { e.UserId, e.SessionId, e.Status })
               .HasDatabaseName("IX_EventAttendances_UserId_SessionId_Status");

        builder.HasIndex(e => new { e.SessionId, e.Status, e.AttendanceType })
               .HasDatabaseName("IX_EventAttendances_SessionId_Status_AttendanceType");

        // GIN index for JSONB metadata
        builder.HasIndex(e => e.Metadata)
               .HasDatabaseName("IX_EventAttendances_Metadata_Gin")
               .HasMethod("gin");

        // Business rule constraints
        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_EventAttendances_AttendanceType",
            "\"AttendanceType\" IN (1, 2)"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_EventAttendances_Status",
            "\"Status\" IN (1, 2, 3, 4)"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_EventAttendances_CancelledAt_Logic",
            "(\"Status\" IN (2, 3) AND \"CancelledAt\" IS NOT NULL) OR (\"Status\" NOT IN (2, 3) AND \"CancelledAt\" IS NULL)"));

        // Partial unique constraint: one ACTIVE attendance per user per event PER TYPE
        // BUSINESS RULE: Users can have both RSVP and Ticket for the same event (social events)
        // AttendanceType included in constraint to allow this combination
        // Allows users to re-RSVP/repurchase after cancelling (cancelled attendances are not constrained)
        builder.HasIndex(e => new { e.UserId, e.EventId, e.AttendanceType })
               .IsUnique()
               .HasDatabaseName("UQ_EventAttendances_User_Event_Type_Active")
               .HasFilter("\"Status\" = 1"); // Only enforce uniqueness for Active attendances (Status = 1)
    }
}
