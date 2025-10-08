using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.CheckIn.Entities.Configuration;

/// <summary>
/// Entity Framework configuration for CheckIn entity
/// </summary>
public class CheckInConfiguration : IEntityTypeConfiguration<CheckIn>
{
    public void Configure(EntityTypeBuilder<CheckIn> builder)
    {
        // Table mapping with check constraints
        builder.ToTable("CheckIns", "public", t =>
        {
            t.HasCheckConstraint("CHK_CheckIns_ManualEntryData",
                    "(\"IsManualEntry\" = true AND \"ManualEntryData\" IS NOT NULL) OR " +
                    "(\"IsManualEntry\" = false AND \"ManualEntryData\" IS NULL)");
        });

        builder.HasKey(c => c.Id);

        // Property configurations
        builder.Property(c => c.CheckInTime)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(c => c.Notes)
               .HasColumnType("text");

        builder.Property(c => c.ManualEntryData)
               .HasColumnType("jsonb");

        builder.Property(c => c.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        // Relationships
        builder.HasOne(c => c.EventAttendee)
               .WithMany(e => e.CheckIns)
               .HasForeignKey(c => c.EventAttendeeId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Event)
               .WithMany()
               .HasForeignKey(c => c.EventId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.StaffMember)
               .WithMany()
               .HasForeignKey(c => c.StaffMemberId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.CreatedByUser)
               .WithMany()
               .HasForeignKey(c => c.CreatedBy)
               .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint - one check-in per attendee
        builder.HasIndex(c => c.EventAttendeeId)
               .IsUnique()
               .HasDatabaseName("UQ_CheckIns_EventAttendee");

        // Indexes
        builder.HasIndex(c => c.EventId)
               .HasDatabaseName("IX_CheckIns_EventId");

        builder.HasIndex(c => c.StaffMemberId)
               .HasDatabaseName("IX_CheckIns_StaffMemberId");

        builder.HasIndex(c => c.CheckInTime)
               .HasDatabaseName("IX_CheckIns_CheckInTime");

        builder.HasIndex(c => new { c.EventId, c.CheckInTime })
               .HasDatabaseName("IX_CheckIns_Event_Time");

        builder.HasIndex(c => c.IsManualEntry)
               .HasDatabaseName("IX_CheckIns_ManualEntry")
               .HasFilter("\"IsManualEntry\" = true");

        builder.HasIndex(c => c.OverrideCapacity)
               .HasDatabaseName("IX_CheckIns_CapacityOverride")
               .HasFilter("\"OverrideCapacity\" = true");

        // JSONB GIN index for manual entry data
        builder.HasIndex(c => c.ManualEntryData)
               .HasDatabaseName("IX_CheckIns_ManualEntryData")
               .HasMethod("gin")
               .HasFilter("\"ManualEntryData\" IS NOT NULL");
    }
}