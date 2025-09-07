using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    public class RSVPConfiguration : IEntityTypeConfiguration<RSVP>
    {
        public void Configure(EntityTypeBuilder<RSVP> builder)
        {
            builder.ToTable("RSVPs");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                .ValueGeneratedNever();

            builder.Property(r => r.UserId)
                .IsRequired();

            builder.Property(r => r.EventId)
                .IsRequired();

            builder.Property(r => r.Status)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(r => r.EmergencyContactName)
                .HasMaxLength(200);

            builder.Property(r => r.EmergencyContactPhone)
                .HasMaxLength(50);

            builder.Property(r => r.CreatedAt)
                .IsRequired();

            builder.Property(r => r.UpdatedAt)
                .IsRequired();

            builder.Property(r => r.CheckedInAt);

            builder.Property(r => r.CheckedInBy);

            builder.Property(r => r.CancelledAt);

            builder.Property(r => r.CancellationReason)
                .HasMaxLength(500);

            builder.Property(r => r.ConfirmationCode)
                .HasMaxLength(20)
                .IsRequired();

            // Configure relationships
            builder.HasOne(r => r.User)
                .WithMany() // No navigation property on User for RSVPs yet
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Event)
                .WithMany(e => e.RSVPs)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(r => r.UserId);
            builder.HasIndex(r => r.EventId);
            builder.HasIndex(r => r.Status);
            builder.HasIndex(r => new { r.UserId, r.EventId }).IsUnique();
            builder.HasIndex(r => r.CreatedAt);
            builder.HasIndex(r => r.ConfirmationCode).IsUnique();
            builder.HasIndex(r => r.CheckedInAt);
        }
    }
}