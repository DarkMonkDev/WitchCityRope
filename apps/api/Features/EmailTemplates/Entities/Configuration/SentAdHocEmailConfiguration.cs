using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.EmailTemplates.Entities.Configuration;

public class SentAdHocEmailConfiguration : IEntityTypeConfiguration<SentAdHocEmail>
{
    public void Configure(EntityTypeBuilder<SentAdHocEmail> builder)
    {
        builder.ToTable("SentAdHocEmails");

        // Primary Key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        // Subject
        builder.Property(e => e.Subject)
            .IsRequired()
            .HasMaxLength(200);

        // HTML Body
        builder.Property(e => e.HtmlBody)
            .IsRequired()
            .HasColumnType("text");

        // Plain Text Body
        builder.Property(e => e.PlainTextBody)
            .IsRequired()
            .HasColumnType("text");

        // Recipient Group
        builder.Property(e => e.RecipientGroup)
            .IsRequired()
            .HasMaxLength(100);

        // Recipient Emails (PostgreSQL array)
        builder.Property(e => e.RecipientEmails)
            .HasColumnType("text[]")
            .HasDefaultValue(Array.Empty<string>());

        // Recipient Count
        builder.Property(e => e.RecipientCount)
            .IsRequired();

        // EventId (nullable, SET NULL on delete)
        builder.Property(e => e.EventId);

        builder.HasOne(e => e.Event)
            .WithMany()
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.SetNull);

        // SendGrid Message ID
        builder.Property(e => e.SendGridMessageId)
            .HasMaxLength(100);

        // Delivery Status
        builder.Property(e => e.DeliveryStatus)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Pending");

        // SentAt (UTC timestamptz)
        builder.Property(e => e.SentAt)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()");

        // Foreign Key to ApplicationUser
        builder.HasOne(e => e.SentByUser)
            .WithMany()
            .HasForeignKey(e => e.SentBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => e.EventId)
            .HasDatabaseName("IX_SentAdHocEmails_EventId")
            .HasFilter("\"EventId\" IS NOT NULL");

        builder.HasIndex(e => e.SentBy)
            .HasDatabaseName("IX_SentAdHocEmails_SentBy");

        builder.HasIndex(e => e.SentAt)
            .IsDescending()
            .HasDatabaseName("IX_SentAdHocEmails_SentAt");

        builder.HasIndex(e => e.DeliveryStatus)
            .HasDatabaseName("IX_SentAdHocEmails_DeliveryStatus")
            .HasFilter("\"DeliveryStatus\" IN ('Pending', 'Failed')");

        // Scheduled send field
        builder.Property(e => e.ScheduledSendAt)
            .IsRequired(false) // Nullable
            .HasColumnType("timestamptz");

        // Partial index for scheduled sends (optimization)
        builder.HasIndex(e => new { e.ScheduledSendAt, e.DeliveryStatus })
            .HasDatabaseName("IX_SentAdHocEmails_Scheduled_Pending")
            .HasFilter("\"ScheduledSendAt\" IS NOT NULL AND \"DeliveryStatus\" = 'Pending'");

        // Check Constraints
        builder.HasCheckConstraint(
            "CHK_SentAdHocEmails_Subject_NotEmpty",
            "LENGTH(TRIM(\"Subject\")) > 0"
        );

        builder.HasCheckConstraint(
            "CHK_SentAdHocEmails_RecipientCount",
            "\"RecipientCount\" >= 0"
        );

        builder.HasCheckConstraint(
            "CHK_SentAdHocEmails_DeliveryStatus",
            "\"DeliveryStatus\" IN ('Pending', 'Scheduled', 'Sent', 'Delivered', 'Failed', 'Bounced')"
        );
    }
}
