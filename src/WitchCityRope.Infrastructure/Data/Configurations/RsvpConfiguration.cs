using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    public class RsvpConfiguration : IEntityTypeConfiguration<Rsvp>
    {
        public void Configure(EntityTypeBuilder<Rsvp> builder)
        {
            builder.ToTable("Rsvps");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                .ValueGeneratedNever();

            builder.Property(r => r.UserId)
                .IsRequired();

            builder.Property(r => r.EventId)
                .IsRequired();

            builder.Property(r => r.RsvpDate)
                .IsRequired();

            builder.Property(r => r.Status)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(r => r.ConfirmationCode)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(r => r.CheckedInAt);

            builder.Property(r => r.CheckedInBy);

            builder.Property(r => r.Notes)
                .HasMaxLength(1000)
                .IsRequired(false);

            builder.Property(r => r.CreatedAt)
                .IsRequired();

            builder.Property(r => r.UpdatedAt)
                .IsRequired();

            // Configure relationships
            // Note: User relationship is now handled through Identity, no direct navigation property
            // The UserId property remains as a foreign key to AspNetUsers table

            builder.HasOne(r => r.Event)
                .WithMany(e => e.Rsvps)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(r => r.UserId);
            builder.HasIndex(r => r.EventId);
            builder.HasIndex(r => r.Status);
            builder.HasIndex(r => new { r.UserId, r.EventId }).IsUnique();
            builder.HasIndex(r => r.RsvpDate);
            builder.HasIndex(r => r.ConfirmationCode).IsUnique();
            builder.HasIndex(r => r.CheckedInAt);
        }
    }
}