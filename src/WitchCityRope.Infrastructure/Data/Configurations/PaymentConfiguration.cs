using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                .ValueGeneratedNever();

            builder.Property(p => p.RegistrationId)
                .IsRequired();

            // Configure Money value object for Amount
            builder.OwnsOne(p => p.Amount, money =>
            {
                money.Property(m => m.Amount)
                    .HasColumnName("Amount")
                    .HasPrecision(18, 2)
                    .IsRequired();

                money.Property(m => m.Currency)
                    .HasColumnName("Currency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            builder.Property(p => p.Status)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(p => p.PaymentMethod)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.TransactionId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.ProcessedAt)
                .IsRequired();

            builder.Property(p => p.UpdatedAt)
                .IsRequired();

            // Configure Money value object for RefundAmount
            builder.OwnsOne(p => p.RefundAmount, money =>
            {
                money.Property(m => m.Amount)
                    .HasColumnName("RefundAmount")
                    .HasPrecision(18, 2);

                money.Property(m => m.Currency)
                    .HasColumnName("RefundCurrency")
                    .HasMaxLength(3);
            });

            builder.Property(p => p.RefundedAt);

            builder.Property(p => p.RefundTransactionId)
                .HasMaxLength(100);

            builder.Property(p => p.RefundReason)
                .HasMaxLength(500);

            // Configure relationships
            builder.HasOne(p => p.Registration)
                .WithOne(r => r.Payment)
                .HasForeignKey<Payment>(p => p.RegistrationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(p => p.TransactionId).IsUnique();
            builder.HasIndex(p => p.Status);
            builder.HasIndex(p => p.ProcessedAt);
            builder.HasIndex(p => p.RefundTransactionId);
        }
    }
}