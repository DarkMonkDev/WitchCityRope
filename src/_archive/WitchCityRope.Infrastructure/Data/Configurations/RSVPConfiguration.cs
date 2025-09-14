using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Entity Framework configuration for RSVP entity
    /// </summary>
    public class RSVPConfiguration : IEntityTypeConfiguration<RSVP>
    {
        public void Configure(EntityTypeBuilder<RSVP> builder)
        {
            builder.ToTable("RSVPs");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.ConfirmationCode)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(r => r.Status)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(r => r.DietaryRestrictions)
                .HasMaxLength(500);

            builder.Property(r => r.CancellationReason)
                .HasMaxLength(500);

            // User relationship
            builder.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Event relationship
            builder.HasOne(r => r.Event)
                .WithMany(e => e.RSVPs)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Optional ticket relationship (when user upgrades RSVP to paid ticket)
            builder.HasOne(r => r.Ticket)
                .WithOne()
                .HasForeignKey<RSVP>(r => r.TicketId)
                .OnDelete(DeleteBehavior.SetNull);

            // Prevent duplicate RSVPs for the same user and event
            // Only active (non-cancelled) RSVPs are considered for uniqueness
            builder.HasIndex(r => new { r.UserId, r.EventId })
                .IsUnique()
                .HasFilter("\"Status\" != 'Cancelled'")
                .HasDatabaseName("IX_RSVPs_UserId_EventId_Active");

            // Unique confirmation codes
            builder.HasIndex(r => r.ConfirmationCode)
                .IsUnique()
                .HasDatabaseName("IX_RSVPs_ConfirmationCode");

            // Performance indexes
            builder.HasIndex(r => r.Status)
                .HasDatabaseName("IX_RSVPs_Status");

            builder.HasIndex(r => r.CreatedAt)
                .HasDatabaseName("IX_RSVPs_CreatedAt");
        }
    }
}