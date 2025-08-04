using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
    {
        public void Configure(EntityTypeBuilder<Ticket> builder)
        {
            builder.ToTable("Tickets");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                .ValueGeneratedNever();

            builder.Property(r => r.UserId)
                .IsRequired();

            builder.Property(r => r.EventId)
                .IsRequired();

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
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(r => r.AccessibilityNeeds)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(r => r.PurchasedAt)
                .IsRequired();

            builder.Property(r => r.ConfirmedAt);

            builder.Property(r => r.CancelledAt);

            builder.Property(r => r.CancellationReason)
                .HasMaxLength(500)
                .IsRequired(false);

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
                .HasMaxLength(200)
                .IsRequired(false);
                
            builder.Property(r => r.EmergencyContactPhone)
                .HasMaxLength(50)
                .IsRequired(false);

            // Configure relationships
            // Note: User relationship is now handled through Identity, no direct navigation property
            // The UserId property remains as a foreign key to AspNetUsers table

            builder.HasOne(r => r.Event)
                .WithMany(e => e.Tickets)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            // TODO: Update Payment entity to use Ticket instead of Registration
            // builder.HasOne(r => r.Payment)
            //     .WithOne(p => p.Ticket)
            //     .HasForeignKey<Payment>(p => p.TicketId)
            //     .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(r => r.UserId);
            builder.HasIndex(r => r.EventId);
            builder.HasIndex(r => r.Status);
            builder.HasIndex(r => new { r.UserId, r.EventId }).IsUnique();
            builder.HasIndex(r => r.PurchasedAt);
            builder.HasIndex(r => r.ConfirmationCode).IsUnique();
            builder.HasIndex(r => r.CheckedInAt);
        }
    }
}