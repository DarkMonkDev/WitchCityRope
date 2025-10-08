using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Api.Features.Payments.Entities;

namespace WitchCityRope.Api.Features.Payments.Data;

/// <summary>
/// Entity Framework configuration for PaymentMethod entity
/// PCI compliant configuration with encrypted storage
/// </summary>
public class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        // Table configuration
        builder.ToTable("PaymentMethods", "public");
        builder.HasKey(pm => pm.Id);

        // ID initialization
        builder.Property(pm => pm.Id)
               .ValueGeneratedOnAdd();

        // Required fields
        builder.Property(pm => pm.UserId)
               .IsRequired();

        #region Encrypted Stripe Data (PCI Compliant)

        builder.Property(pm => pm.EncryptedStripePaymentMethodId)
               .IsRequired()
               .HasColumnType("text");

        #endregion

        #region Display Information (Safe to Store)

        builder.Property(pm => pm.LastFourDigits)
               .IsRequired()
               .HasMaxLength(4);

        builder.Property(pm => pm.CardBrand)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(pm => pm.ExpiryMonth)
               .IsRequired();

        builder.Property(pm => pm.ExpiryYear)
               .IsRequired();

        #endregion

        #region User Preferences

        builder.Property(pm => pm.IsDefault)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(pm => pm.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        #endregion

        #region DateTime Configuration

        builder.Property(pm => pm.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz")
               .HasDefaultValueSql("NOW()");

        builder.Property(pm => pm.UpdatedAt)
               .IsRequired()
               .HasColumnType("timestamptz")
               .HasDefaultValueSql("NOW()");

        #endregion

        #region Foreign Key Relationships

        builder.HasOne(pm => pm.User)
               .WithMany()
               .HasForeignKey(pm => pm.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region Performance Indexes

        builder.HasIndex(pm => pm.UserId)
               .HasDatabaseName("IX_PaymentMethods_UserId");

        builder.HasIndex(pm => new { pm.UserId, pm.IsDefault })
               .HasDatabaseName("IX_PaymentMethods_IsDefault")
               .HasFilter("\"IsDefault\" = true");

        builder.HasIndex(pm => new { pm.UserId, pm.IsActive })
               .HasDatabaseName("IX_PaymentMethods_IsActive")
               .HasFilter("\"IsActive\" = true");

        // Unique constraint for default payment method per user
        builder.HasIndex(pm => pm.UserId)
               .IsUnique()
               .HasDatabaseName("UX_PaymentMethods_UserDefault")
               .HasFilter("\"IsDefault\" = true AND \"IsActive\" = true");

        #endregion

        #region Business Rule Constraints

        // Card brand validation
        builder.HasCheckConstraint("CHK_PaymentMethods_CardBrand",
            "\"CardBrand\" IN ('Visa', 'MasterCard', 'American Express', 'Discover', 'JCB', 'Diners Club')");

        // Last four digits validation (must be 4 digits)
        builder.HasCheckConstraint("CHK_PaymentMethods_LastFourDigits",
            "\"LastFourDigits\" ~ '^\\d{4}$'");

        // Expiry month validation (1-12)
        builder.HasCheckConstraint("CHK_PaymentMethods_ExpiryMonth_Range",
            "\"ExpiryMonth\" >= 1 AND \"ExpiryMonth\" <= 12");

        // Expiry year validation (current year or future)
        builder.HasCheckConstraint("CHK_PaymentMethods_ExpiryYear_Future",
            "\"ExpiryYear\" >= EXTRACT(YEAR FROM NOW())");

        #endregion
    }
}