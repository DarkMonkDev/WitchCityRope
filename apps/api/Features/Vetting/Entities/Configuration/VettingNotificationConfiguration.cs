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
        builder.ToTable("VettingNotifications", "public");
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.RecipientEmail)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Subject)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Body)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.ErrorMessage)
            .HasColumnType("text");

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(e => e.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(e => e.SentAt)
            .HasColumnType("timestamptz");

        // Relationships
        builder.HasOne(e => e.Application)
            .WithMany()
            .HasForeignKey(e => e.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Template)
            .WithMany(t => t.Notifications)
            .HasForeignKey(e => e.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(e => e.ApplicationId)
            .HasDatabaseName("IX_VettingNotifications_ApplicationId");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_VettingNotifications_Status");

        builder.HasIndex(e => new { e.Status, e.CreatedAt })
            .HasDatabaseName("IX_VettingNotifications_Status_CreatedAt");

        builder.HasIndex(e => e.RecipientEmail)
            .HasDatabaseName("IX_VettingNotifications_RecipientEmail");
    }
}