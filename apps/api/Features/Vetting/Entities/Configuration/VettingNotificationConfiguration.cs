using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.Vetting.Entities.Configuration;

/// <summary>
/// Entity Framework configuration for VettingNotification
/// </summary>
public class VettingNotificationConfiguration : IEntityTypeConfiguration<VettingNotification>
{
    public void Configure(EntityTypeBuilder<VettingNotification> builder)
    {
        // Table mapping
        builder.ToTable("VettingNotifications", "public");
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.NotificationType)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(e => e.RecipientEmail)
               .IsRequired()
               .HasMaxLength(255);

        builder.Property(e => e.Subject)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(e => e.MessageBody)
               .IsRequired()
               .HasColumnType("text");

        builder.Property(e => e.Status)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(e => e.ErrorMessage)
               .HasColumnType("text");

        builder.Property(e => e.MessageId)
               .HasMaxLength(200);

        builder.Property(e => e.DeliveryStatus)
               .HasMaxLength(50);

        builder.Property(e => e.TemplateName)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(e => e.TemplateData)
               .HasColumnType("jsonb");

        // DateTime properties
        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.UpdatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.SentAt)
               .HasColumnType("timestamptz");

        builder.Property(e => e.NextRetryAt)
               .HasColumnType("timestamptz");

        // Indexes
        builder.HasIndex(e => e.ApplicationId)
               .HasDatabaseName("IX_VettingNotifications_ApplicationId");

        builder.HasIndex(e => new { e.Status, e.CreatedAt })
               .HasDatabaseName("IX_VettingNotifications_Status_CreatedAt");

        builder.HasIndex(e => e.NotificationType)
               .HasDatabaseName("IX_VettingNotifications_NotificationType");

        // Partial index for failed notifications
        builder.HasIndex(e => new { e.CreatedAt, e.RetryCount })
               .HasDatabaseName("IX_VettingNotifications_Failed_RetryCount")
               .HasFilter("\"Status\" = 4 AND \"RetryCount\" < 5"); // Failed status with retry limit

        // Performance index for delivery queue processing
        builder.HasIndex(e => new { e.Status, e.NextRetryAt, e.CreatedAt })
               .HasDatabaseName("IX_VettingNotifications_DeliveryQueue")
               .HasFilter("\"Status\" IN (1, 4)"); // Pending, Failed

        // GIN index for template data search
        builder.HasIndex(e => e.TemplateData)
               .HasDatabaseName("IX_VettingNotifications_TemplateData")
               .HasMethod("gin");

        // Relationships
        builder.HasOne(e => e.Application)
               .WithMany(a => a.Notifications)
               .HasForeignKey(e => e.ApplicationId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}