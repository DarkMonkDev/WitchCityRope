using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.Vetting.Entities.Configuration;

/// <summary>
/// Entity Framework configuration for VettingEmailLog
/// Optimized for SendGrid delivery tracking and query performance
/// </summary>
public class VettingEmailLogConfiguration : IEntityTypeConfiguration<VettingEmailLog>
{
    public void Configure(EntityTypeBuilder<VettingEmailLog> builder)
    {
        // Table mapping
        builder.ToTable("VettingEmailLogs", "public");
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.ApplicationId)
               .IsRequired();

        builder.Property(e => e.TemplateType)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(e => e.RecipientEmail)
               .IsRequired()
               .HasMaxLength(256);

        builder.Property(e => e.Subject)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(e => e.DeliveryStatus)
               .IsRequired()
               .HasConversion<int>()
               .HasDefaultValue(EmailDeliveryStatus.Pending);

        builder.Property(e => e.SendGridMessageId)
               .HasMaxLength(100);

        builder.Property(e => e.ErrorMessage)
               .HasColumnType("text");

        builder.Property(e => e.RetryCount)
               .IsRequired()
               .HasDefaultValue(0);

        // DateTime properties - CRITICAL: Use timestamptz for PostgreSQL
        builder.Property(e => e.SentAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.LastRetryAt)
               .HasColumnType("timestamptz");

        // Relationships
        builder.HasOne(e => e.Application)
               .WithMany()
               .HasForeignKey(e => e.ApplicationId)
               .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance

        // Index for application history queries
        builder.HasIndex(e => e.ApplicationId)
               .HasDatabaseName("IX_VettingEmailLogs_ApplicationId");

        // Index for delivery status monitoring
        builder.HasIndex(e => e.DeliveryStatus)
               .HasDatabaseName("IX_VettingEmailLogs_DeliveryStatus");

        // Index for date range queries
        builder.HasIndex(e => e.SentAt)
               .HasDatabaseName("IX_VettingEmailLogs_SentAt");

        // Composite index for failed email retry queries
        builder.HasIndex(e => new { e.DeliveryStatus, e.RetryCount, e.SentAt })
               .HasDatabaseName("IX_VettingEmailLogs_DeliveryStatus_RetryCount_SentAt")
               .HasFilter("\"DeliveryStatus\" IN (3, 4) AND \"RetryCount\" < 5");

        // Index for SendGrid message ID lookups
        builder.HasIndex(e => e.SendGridMessageId)
               .HasDatabaseName("IX_VettingEmailLogs_SendGridMessageId")
               .HasFilter("\"SendGridMessageId\" IS NOT NULL");

        // Check constraints
        builder.ToTable("VettingEmailLogs", t => t.HasCheckConstraint(
            "CHK_VettingEmailLogs_RetryCount",
            "\"RetryCount\" >= 0 AND \"RetryCount\" <= 10"
        ));
    }
}
