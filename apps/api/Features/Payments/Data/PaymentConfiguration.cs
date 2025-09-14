using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Api.Features.Payments.Entities;
using WitchCityRope.Api.Features.Payments.Models;

namespace WitchCityRope.Api.Features.Payments.Data;

/// <summary>
/// Entity Framework configuration for Payment entity
/// Follows PostgreSQL best practices with proper indexing and constraints
/// </summary>
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        // Table configuration
        builder.ToTable("Payments", "public");
        builder.HasKey(p => p.Id);
        
        // ID initialization to prevent duplicate key violations
        builder.Property(p => p.Id)
               .ValueGeneratedOnAdd();
        
        // Required fields
        builder.Property(p => p.EventRegistrationId)
               .IsRequired();
               
        builder.Property(p => p.UserId)
               .IsRequired();
        
        #region Money Value Object Configuration (PostgreSQL optimized)
        
        // Store money as separate decimal and currency fields for PostgreSQL efficiency
        builder.Property(p => p.AmountValue)
               .IsRequired()
               .HasColumnType("decimal(10,2)")
               .HasColumnName("AmountValue");
               
        builder.Property(p => p.Currency)
               .IsRequired()
               .HasMaxLength(3)
               .HasDefaultValue("USD")
               .HasColumnName("Currency");
        
        #endregion
        
        #region Sliding Scale Pricing
        
        builder.Property(p => p.SlidingScalePercentage)
               .IsRequired()
               .HasColumnType("decimal(5,2)")
               .HasDefaultValue(0.00m);
        
        #endregion
        
        #region Payment Processing
        
        builder.Property(p => p.Status)
               .IsRequired()
               .HasConversion<int>();
               
        builder.Property(p => p.PaymentMethodType)
               .IsRequired()
               .HasConversion<int>();
        
        // Encrypted fields for PCI compliance
        builder.Property(p => p.EncryptedPayPalOrderId)
               .HasColumnType("text");
               
        builder.Property(p => p.EncryptedPayPalPayerId)
               .HasColumnType("text");
               
        builder.Property(p => p.VenmoUsername)
               .HasColumnType("text");
        
        #endregion
        
        #region DateTime Configuration (PostgreSQL TIMESTAMPTZ)
        
        builder.Property(p => p.ProcessedAt)
               .HasColumnType("timestamptz");
               
        builder.Property(p => p.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz")
               .HasDefaultValueSql("NOW()");
               
        builder.Property(p => p.UpdatedAt)
               .IsRequired()
               .HasColumnType("timestamptz")
               .HasDefaultValueSql("NOW()");
        
        #endregion
        
        #region Refund Configuration
        
        builder.Property(p => p.RefundAmountValue)
               .HasColumnType("decimal(10,2)");
               
        builder.Property(p => p.RefundCurrency)
               .HasMaxLength(3);
               
        builder.Property(p => p.RefundedAt)
               .HasColumnType("timestamptz");
               
        builder.Property(p => p.EncryptedPayPalRefundId)
               .HasColumnType("text");
        
        builder.Property(p => p.RefundReason)
               .HasColumnType("text");
        
        #endregion
        
        #region JSONB Metadata
        
        builder.Property(p => p.Metadata)
               .IsRequired()
               .HasColumnType("jsonb")
               .HasDefaultValueSql("'{}'");
        
        #endregion
        
        #region Foreign Key Relationships
        
        builder.HasOne(p => p.User)
               .WithMany()
               .HasForeignKey(p => p.UserId)
               .OnDelete(DeleteBehavior.Restrict);
               
        builder.HasOne(p => p.RefundedByUser)
               .WithMany()
               .HasForeignKey(p => p.RefundedByUserId)
               .OnDelete(DeleteBehavior.SetNull);
        
        #endregion
        
        #region Navigation Properties
        
        builder.HasMany(p => p.AuditLogs)
               .WithOne(a => a.Payment)
               .HasForeignKey(a => a.PaymentId)
               .OnDelete(DeleteBehavior.Cascade);
               
        builder.HasMany(p => p.Refunds)
               .WithOne(r => r.OriginalPayment)
               .HasForeignKey(r => r.OriginalPaymentId)
               .OnDelete(DeleteBehavior.Cascade);
               
        builder.HasMany(p => p.Failures)
               .WithOne(f => f.Payment)
               .HasForeignKey(f => f.PaymentId)
               .OnDelete(DeleteBehavior.Cascade);
        
        #endregion
        
        #region Performance Indexes
        
        // Core lookup indexes
        builder.HasIndex(p => p.UserId)
               .HasDatabaseName("IX_Payments_UserId");
               
        builder.HasIndex(p => p.EventRegistrationId)
               .HasDatabaseName("IX_Payments_EventRegistrationId");
               
        builder.HasIndex(p => p.Status)
               .HasDatabaseName("IX_Payments_Status");
               
        builder.HasIndex(p => p.ProcessedAt)
               .HasDatabaseName("IX_Payments_ProcessedAt");
               
        builder.HasIndex(p => p.CreatedAt)
               .HasDatabaseName("IX_Payments_CreatedAt");
               
        builder.HasIndex(p => p.SlidingScalePercentage)
               .HasDatabaseName("IX_Payments_SlidingScalePercentage");
        
        // Partial indexes for performance on specific queries
        builder.HasIndex(p => p.CreatedAt)
               .HasDatabaseName("IX_Payments_PendingStatus")
               .HasFilter("\"Status\" = 0"); // Pending payments only
               
        builder.HasIndex(p => p.ProcessedAt)
               .HasDatabaseName("IX_Payments_FailedStatus")
               .HasFilter("\"Status\" = 2"); // Failed payments only
               
        builder.HasIndex(p => p.RefundedAt)
               .HasDatabaseName("IX_Payments_RefundedStatus")
               .HasFilter("\"RefundedAt\" IS NOT NULL"); // Refunded payments only
        
        // GIN index for JSONB metadata queries
        builder.HasIndex(p => p.Metadata)
               .HasDatabaseName("IX_Payments_Metadata_Gin")
               .HasMethod("gin");
        
        // Unique constraint to prevent duplicate payment processing per registration
        builder.HasIndex(p => p.EventRegistrationId)
               .IsUnique()
               .HasDatabaseName("UX_Payments_EventRegistration_Completed")
               .HasFilter("\"Status\" = 1"); // Only one completed payment per registration
        
        #endregion
        
        #region Business Rule Constraints
        
        // Amount must be non-negative
        builder.HasCheckConstraint("CHK_Payments_AmountValue_NonNegative", 
            "\"AmountValue\" >= 0");
            
        // Sliding scale percentage must be between 0 and 75
        builder.HasCheckConstraint("CHK_Payments_SlidingScalePercentage_Range", 
            "\"SlidingScalePercentage\" >= 0 AND \"SlidingScalePercentage\" <= 75.00");
            
        // Currency must be valid
        builder.HasCheckConstraint("CHK_Payments_Currency_Valid", 
            "\"Currency\" IN ('USD', 'EUR', 'GBP', 'CAD')");
            
        // Refund currency consistency
        builder.HasCheckConstraint("CHK_Payments_RefundCurrency_Valid", 
            "\"RefundCurrency\" IS NULL OR \"RefundCurrency\" IN ('USD', 'EUR', 'GBP', 'CAD')");
            
        // Refund amount cannot exceed original payment
        builder.HasCheckConstraint("CHK_Payments_RefundAmount_NotExceedOriginal", 
            "\"RefundAmountValue\" IS NULL OR \"RefundAmountValue\" <= \"AmountValue\"");
            
        // Refund consistency - if refund amount exists, refunded date must exist
        builder.HasCheckConstraint("CHK_Payments_RefundRequiresOriginalPayment", 
            "(\"RefundAmountValue\" IS NULL AND \"RefundedAt\" IS NULL) OR (\"RefundAmountValue\" IS NOT NULL AND \"RefundedAt\" IS NOT NULL)");
            
        // Currency consistency between original and refund
        builder.HasCheckConstraint("CHK_Payments_CurrencyConsistency",
            "(\"RefundCurrency\" IS NULL) OR (\"RefundCurrency\" = \"Currency\")");
        
        #endregion
    }
}