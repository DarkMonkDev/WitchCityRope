using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.CheckIn.Entities.Configuration;

/// <summary>
/// Entity Framework configuration for EventAttendee entity
/// </summary>
public class EventAttendeeConfiguration : IEntityTypeConfiguration<EventAttendee>
{
    public void Configure(EntityTypeBuilder<EventAttendee> builder)
    {
        // Table mapping
        builder.ToTable("EventAttendees", "public");
        builder.HasKey(e => e.Id);

        // Property configurations
        builder.Property(e => e.RegistrationStatus)
               .IsRequired()
               .HasColumnType("text");

        builder.Property(e => e.TicketNumber)
               .HasMaxLength(50);

        builder.Property(e => e.DietaryRestrictions)
               .HasColumnType("text");

        builder.Property(e => e.AccessibilityNeeds)
               .HasColumnType("text");

        builder.Property(e => e.EmergencyContactName)
               .HasMaxLength(200);

        builder.Property(e => e.EmergencyContactPhone)
               .HasMaxLength(50);

        builder.Property(e => e.Metadata)
               .IsRequired()
               .HasColumnType("jsonb")
               .HasDefaultValue("{}");

        // DateTime properties with timestamptz
        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.UpdatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        // Relationships
        builder.HasOne(e => e.Event)
               .WithMany()
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

        builder.HasMany(e => e.CheckIns)
               .WithOne(c => c.EventAttendee)
               .HasForeignKey(c => c.EventAttendeeId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.AuditLogs)
               .WithOne(a => a.EventAttendee)
               .HasForeignKey(a => a.EventAttendeeId)
               .OnDelete(DeleteBehavior.SetNull);

        // Constraints
        builder.HasCheckConstraint("CHK_EventAttendees_WaitlistPosition",
                "\"WaitlistPosition\" > 0 OR \"WaitlistPosition\" IS NULL");

        builder.HasCheckConstraint("CHK_EventAttendees_RegistrationStatus",
                "\"RegistrationStatus\" IN ('confirmed', 'waitlist', 'checked-in', 'no-show', 'cancelled')");

        // Indexes
        builder.HasIndex(e => e.EventId)
               .HasDatabaseName("IX_EventAttendees_EventId");

        builder.HasIndex(e => e.UserId)
               .HasDatabaseName("IX_EventAttendees_UserId");

        builder.HasIndex(e => e.RegistrationStatus)
               .HasDatabaseName("IX_EventAttendees_RegistrationStatus");

        builder.HasIndex(e => new { e.EventId, e.RegistrationStatus })
               .HasDatabaseName("IX_EventAttendees_Event_Status");

        builder.HasIndex(e => new { e.EventId, e.WaitlistPosition })
               .HasDatabaseName("IX_EventAttendees_Event_Waitlist")
               .HasFilter("\"RegistrationStatus\" = 'waitlist'");

        builder.HasIndex(e => e.IsFirstTime)
               .HasDatabaseName("IX_EventAttendees_FirstTime")
               .HasFilter("\"IsFirstTime\" = true");

        builder.HasIndex(e => e.HasCompletedWaiver)
               .HasDatabaseName("IX_EventAttendees_Waiver")
               .HasFilter("\"HasCompletedWaiver\" = false");

        // JSONB GIN index
        builder.HasIndex(e => e.Metadata)
               .HasDatabaseName("IX_EventAttendees_Metadata")
               .HasMethod("gin");

        // Unique constraints
        builder.HasIndex(e => new { e.EventId, e.UserId })
               .IsUnique()
               .HasDatabaseName("UQ_EventAttendees_EventUser");

        builder.HasIndex(e => e.TicketNumber)
               .IsUnique()
               .HasDatabaseName("IX_EventAttendees_TicketNumber_Unique")
               .HasFilter("\"TicketNumber\" IS NOT NULL");
    }
}