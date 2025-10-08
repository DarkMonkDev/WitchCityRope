using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Api.Features.Payments.Entities;
using WitchCityRope.Api.Features.Payments.Models;

namespace WitchCityRope.Api.Features.Payments.Data;

/// <summary>
/// Entity Framework configuration for PaymentRefund entity
/// Comprehensive refund tracking with business rule enforcement
/// </summary>
public class PaymentRefundConfiguration : IEntityTypeConfiguration<PaymentRefund>
{
    public void Configure(EntityTypeBuilder<PaymentRefund> builder)
    {
        // Table configuration with check constraints
        builder.ToTable("PaymentRefunds", "public", t =>
        {
            // Refund amount must be positive
            t.HasCheckConstraint("CHK_PaymentRefunds_RefundAmountValue_Positive",
                "\"RefundAmountValue\" > 0");

            // Currency validation
            t.HasCheckConstraint("CHK_PaymentRefunds_Currency",
                "\"RefundCurrency\" IN ('USD', 'EUR', 'GBP', 'CAD')");

            // Refund reason minimum length (for audit compliance)
            t.HasCheckConstraint("CHK_PaymentRefunds_ReasonRequired",
                "LENGTH(TRIM(\"RefundReason\")) >= 10");
        });

        builder.HasKey(r => r.Id);

        // ID initialization
        builder.Property(r => r.Id)
               .ValueGeneratedOnAdd();

        // Required fields
        builder.Property(r => r.OriginalPaymentId)
               .IsRequired();

        builder.Property(r => r.ProcessedByUserId)
               .IsRequired();

        #region Refund Amount Configuration

        builder.Property(r => r.RefundAmountValue)
               .IsRequired()
               .HasColumnType("decimal(10,2)");

        builder.Property(r => r.RefundCurrency)
               .IsRequired()
               .HasMaxLength(3)
               .HasDefaultValue("USD");

        #endregion

        #region Refund Details

        builder.Property(r => r.RefundReason)
               .IsRequired()
               .HasColumnType("text");

        builder.Property(r => r.RefundStatus)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(r => r.EncryptedPayPalRefundId)
               .HasColumnType("text");

        #endregion

        #region DateTime Configuration

        builder.Property(r => r.ProcessedAt)
               .IsRequired()
               .HasColumnType("timestamptz")
               .HasDefaultValueSql("NOW()");

        builder.Property(r => r.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz")
               .HasDefaultValueSql("NOW()");

        #endregion

        #region JSONB Metadata

        builder.Property(r => r.Metadata)
               .IsRequired()
               .HasConversion(
                   v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                   v => v == null ? new Dictionary<string, object>() :
                        System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
               );

        // PostgreSQL-specific configuration (skip for InMemory)
        try
        {
            builder.Property(r => r.Metadata)
                   .HasColumnType("jsonb")
                   .HasDefaultValueSql("'{}'");
        }
        catch
        {
            // Skip column type configuration for InMemory database
        }

        #endregion

        #region Foreign Key Relationships

        builder.HasOne(r => r.OriginalPayment)
               .WithMany(p => p.Refunds)
               .HasForeignKey(r => r.OriginalPaymentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.ProcessedByUser)
               .WithMany()
               .HasForeignKey(r => r.ProcessedByUserId)
               .OnDelete(DeleteBehavior.Restrict);

        #endregion

        #region Performance Indexes

        builder.HasIndex(r => r.OriginalPaymentId)
               .HasDatabaseName("IX_PaymentRefunds_OriginalPaymentId");

        builder.HasIndex(r => r.ProcessedByUserId)
               .HasDatabaseName("IX_PaymentRefunds_ProcessedByUserId");

        builder.HasIndex(r => r.ProcessedAt)
               .HasDatabaseName("IX_PaymentRefunds_ProcessedAt");

        builder.HasIndex(r => r.RefundStatus)
               .HasDatabaseName("IX_PaymentRefunds_RefundStatus");

        // GIN index for metadata
        builder.HasIndex(r => r.Metadata)
               .HasDatabaseName("IX_PaymentRefunds_Metadata_Gin")
               .HasMethod("gin");

        #endregion
    }
}