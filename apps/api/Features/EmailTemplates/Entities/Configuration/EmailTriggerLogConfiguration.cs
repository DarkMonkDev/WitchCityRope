using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.EmailTemplates.Entities.Configuration;

public class EmailTriggerLogConfiguration : IEntityTypeConfiguration<EmailTriggerLog>
{
    public void Configure(EntityTypeBuilder<EmailTriggerLog> builder)
    {
        builder.ToTable("EmailTriggerLogs");

        // Primary Key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        // Template ID
        builder.Property(e => e.TemplateId)
            .IsRequired();

        // Event ID (nullable)
        builder.Property(e => e.EventId)
            .IsRequired(false);

        // Session ID (nullable)
        builder.Property(e => e.SessionId)
            .IsRequired(false);

        // Template Type
        builder.Property(e => e.TemplateType)
            .IsRequired()
            .HasMaxLength(50);

        // Trigger Type
        builder.Property(e => e.TriggerType)
            .IsRequired()
            .HasMaxLength(20);

        // Recipient Group
        builder.Property(e => e.RecipientGroup)
            .IsRequired()
            .HasMaxLength(50);

        // Recipient Count
        builder.Property(e => e.RecipientCount)
            .IsRequired();

        // Timestamps (UTC timestamptz)
        builder.Property(e => e.TriggeredAt)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.SentAt)
            .IsRequired(false)
            .HasColumnType("timestamptz");

        // Status
        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Sent");

        // Error Message
        builder.Property(e => e.ErrorMessage)
            .IsRequired(false)
            .HasColumnType("text");

        // Foreign Keys (nullable to avoid cascade issues)
        builder.HasOne(e => e.Event)
            .WithMany()
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Session)
            .WithMany()
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for query performance
        builder.HasIndex(e => e.TemplateId)
            .HasDatabaseName("IX_EmailTriggerLogs_TemplateId");

        builder.HasIndex(e => new { e.EventId, e.SessionId })
            .HasDatabaseName("IX_EmailTriggerLogs_EventId_SessionId");

        builder.HasIndex(e => e.TriggeredAt)
            .IsDescending()
            .HasDatabaseName("IX_EmailTriggerLogs_TriggeredAt");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_EmailTriggerLogs_Status");

        // Idempotency index (prevent duplicate sends)
        builder.HasIndex(e => new { e.TemplateId, e.SessionId, e.TemplateType })
            .IsUnique()
            .HasDatabaseName("UQ_EmailTriggerLogs_Idempotency")
            .HasFilter("\"SessionId\" IS NOT NULL AND \"Status\" = 'Sent'");

        // Partial index for failed sends
        builder.HasIndex(e => new { e.Status, e.TriggeredAt })
            .HasDatabaseName("IX_EmailTriggerLogs_Failed_TriggeredAt")
            .HasFilter("\"Status\" = 'Failed'");

        // Check Constraints
        builder.HasCheckConstraint(
            "CHK_EmailTriggerLogs_Status",
            "\"Status\" IN ('Sent', 'Failed', 'Skipped')"
        );

        builder.HasCheckConstraint(
            "CHK_EmailTriggerLogs_RecipientCount",
            "\"RecipientCount\" >= 0"
        );
    }
}
