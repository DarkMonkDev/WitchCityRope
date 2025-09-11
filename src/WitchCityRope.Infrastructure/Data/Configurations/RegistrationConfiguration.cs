using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.ValueObjects;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    public class RegistrationConfiguration : IEntityTypeConfiguration<Registration>
    {
        public void Configure(EntityTypeBuilder<Registration> builder)
        {
            builder.ToTable("Registrations");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                .ValueGeneratedNever();

            builder.Property(r => r.UserId)
                .IsRequired();

            builder.Property(r => r.EventId)
                .IsRequired();

            // Optional EventTicketTypeId for new session-based registrations
            builder.Property(r => r.EventTicketTypeId)
                .IsRequired(false);

            // Configure Money value object for SelectedPrice
            builder.OwnsOne(r => r.SelectedPrice, money =>
            {
                money.Property(m => m.Amount)
                    .HasColumnName("SelectedPriceAmount")
                    .HasPrecision(18, 2)
                    .IsRequired();

                money.Property(m => m.Currency)
                    .HasColumnName("SelectedPriceCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            builder.Property(r => r.Status)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(r => r.DietaryRestrictions)
                .HasMaxLength(500);

            builder.Property(r => r.AccessibilityNeeds)
                .HasMaxLength(500);

            builder.Property(r => r.RegisteredAt)
                .IsRequired();

            builder.Property(r => r.ConfirmedAt);

            builder.Property(r => r.CancelledAt);

            builder.Property(r => r.CancellationReason)
                .HasMaxLength(500);

            builder.Property(r => r.UpdatedAt)
                .IsRequired();
                
            builder.Property(r => r.CreatedAt)
                .IsRequired();
                
            builder.Property(r => r.CheckedInAt);
            
            builder.Property(r => r.CheckedInBy);
            
            builder.Property(r => r.ConfirmationCode)
                .HasMaxLength(50)
                .IsRequired();
                
            builder.Property(r => r.EmergencyContactName)
                .HasMaxLength(200);
                
            builder.Property(r => r.EmergencyContactPhone)
                .HasMaxLength(50);

            // Configure relationships
            builder.HasOne(r => r.User)
                .WithMany(u => u.Registrations)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Event)
                .WithMany(e => e.Registrations)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Payment)
                .WithOne(p => p.Registration)
                .HasForeignKey<Payment>(p => p.RegistrationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(r => r.UserId);
            builder.HasIndex(r => r.EventId);
            builder.HasIndex(r => r.Status);
            builder.HasIndex(r => new { r.UserId, r.EventId }).IsUnique();
            builder.HasIndex(r => r.RegisteredAt);
            builder.HasIndex(r => r.ConfirmationCode).IsUnique();
            builder.HasIndex(r => r.CheckedInAt);
        }
    }
}