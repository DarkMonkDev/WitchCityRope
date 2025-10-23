using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Api.Features.Payments.Entities;

namespace WitchCityRope.Api.Features.Payments.Configuration;

/// <summary>
/// Entity Framework configuration for PaymentFailure entity
/// Detailed failure tracking for troubleshooting and retry mechanisms
/// </summary>
public class PaymentFailureConfiguration : IEntityTypeConfiguration<PaymentFailure>
{
    public void Configure(EntityTypeBuilder<PaymentFailure> builder)
    {
        // Table configuration with check constraints
        builder.ToTable("PaymentFailures", "public", t =>
        {
            // Retry count must be non-negative
            t.HasCheckConstraint("CHK_PaymentFailures_RetryCount_NonNegative",
                "\"RetryCount\" >= 0");
        });

        builder.HasKey(f => f.Id);

        // ID initialization
        builder.Property(f => f.Id)
               .ValueGeneratedOnAdd();

        // Required fields
        builder.Property(f => f.PaymentId)
               .IsRequired();

        #region Failure Details

        builder.Property(f => f.FailureCode)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(f => f.FailureMessage)
               .IsRequired()
               .HasColumnType("text");

        builder.Property(f => f.EncryptedStripeErrorDetails)
               .HasColumnType("text");

        #endregion

        #region Retry Tracking

        builder.Property(f => f.RetryCount)
               .IsRequired()
               .HasDefaultValue(0);

        #endregion

        #region DateTime Configuration

        builder.Property(f => f.FailedAt)
               .IsRequired()
               .HasColumnType("timestamptz")
               .HasDefaultValueSql("NOW()");

        builder.Property(f => f.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz")
               .HasDefaultValueSql("NOW()");

        #endregion

        #region Foreign Key Relationships

        builder.HasOne(f => f.Payment)
               .WithMany(p => p.Failures)
               .HasForeignKey(f => f.PaymentId)
               .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region Performance Indexes

        builder.HasIndex(f => f.PaymentId)
               .HasDatabaseName("IX_PaymentFailures_PaymentId");

        builder.HasIndex(f => f.FailureCode)
               .HasDatabaseName("IX_PaymentFailures_FailureCode");

        builder.HasIndex(f => f.FailedAt)
               .HasDatabaseName("IX_PaymentFailures_FailedAt");

        // Partial index for failures with retries (troubleshooting focus)
        builder.HasIndex(f => f.RetryCount)
               .HasDatabaseName("IX_PaymentFailures_RetryCount")
               .HasFilter("\"RetryCount\" > 0");

        #endregion
    }
}