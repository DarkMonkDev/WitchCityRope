using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Api.Features.Participation.Entities;

namespace WitchCityRope.Api.Features.Participation.Configuration;

/// <summary>
/// Entity Framework configuration for EventParticipation entity
/// Implements PostgreSQL-specific patterns and constraints
/// </summary>
public class EventParticipationConfiguration : IEntityTypeConfiguration<EventParticipation>
{
    public void Configure(EntityTypeBuilder<EventParticipation> builder)
    {
        // Table mapping
        builder.ToTable("EventParticipations", "public");
        builder.HasKey(e => e.Id);

        // Property configurations with PostgreSQL patterns
        builder.Property(e => e.Id)
               .ValueGeneratedOnAdd(); // Let PostgreSQL generate UUIDs

        builder.Property(e => e.EventId)
               .IsRequired();

        builder.Property(e => e.UserId)
               .IsRequired();

        builder.Property(e => e.ParticipationType)
               .IsRequired()
               .HasConversion<int>(); // Store as INTEGER

        builder.Property(e => e.Status)
               .IsRequired()
               .HasConversion<int>(); // Store as INTEGER

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

        // Foreign key relationships
        builder.HasOne(e => e.Event)
               .WithMany(e => e.EventParticipations)
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

        // One-to-many relationship with ParticipationHistory
        builder.HasMany(e => e.History)
               .WithOne(h => h.Participation)
               .HasForeignKey(h => h.ParticipationId)
               .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(e => new { e.EventId, e.Status })
               .HasDatabaseName("IX_EventParticipations_EventId_Status");

        builder.HasIndex(e => new { e.UserId, e.Status })
               .HasDatabaseName("IX_EventParticipations_UserId_Status");

        builder.HasIndex(e => e.CreatedAt)
               .HasDatabaseName("IX_EventParticipations_CreatedAt");

        // GIN index for JSONB metadata
        builder.HasIndex(e => e.Metadata)
               .HasDatabaseName("IX_EventParticipations_Metadata_Gin")
               .HasMethod("gin");

        // Business rule constraints
        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_EventParticipations_ParticipationType",
            "\"ParticipationType\" IN (1, 2)"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_EventParticipations_Status",
            "\"Status\" IN (1, 2, 3, 4)"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_EventParticipations_CancelledAt_Logic",
            "(\"Status\" IN (2, 3) AND \"CancelledAt\" IS NOT NULL) OR (\"Status\" NOT IN (2, 3) AND \"CancelledAt\" IS NULL)"));

        // Partial unique constraint: one ACTIVE participation per user per event PER TYPE
        // BUSINESS RULE: Users can have both RSVP and Ticket for the same event (social events)
        // ParticipationType included in constraint to allow this combination
        // Allows users to re-RSVP/repurchase after cancelling (cancelled participations are not constrained)
        builder.HasIndex(e => new { e.UserId, e.EventId, e.ParticipationType })
               .IsUnique()
               .HasDatabaseName("UQ_EventParticipations_User_Event_Type_Active")
               .HasFilter("\"Status\" = 1"); // Only enforce uniqueness for Active participations (Status = 1)
    }
}