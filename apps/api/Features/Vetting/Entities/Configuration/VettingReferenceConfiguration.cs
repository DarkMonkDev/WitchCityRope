using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.Vetting.Entities.Configuration;

/// <summary>
/// Entity Framework configuration for VettingReference
/// </summary>
public class VettingReferenceConfiguration : IEntityTypeConfiguration<VettingReference>
{
    public void Configure(EntityTypeBuilder<VettingReference> builder)
    {
        // Table mapping
        builder.ToTable("VettingReferences", "public");
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.EncryptedName)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(e => e.EncryptedEmail)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(e => e.EncryptedRelationship)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(e => e.ResponseToken)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(e => e.Status)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(e => e.ManualContactNotes)
               .HasMaxLength(1000);

        // DateTime properties
        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.UpdatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.ContactedAt)
               .HasColumnType("timestamptz");

        builder.Property(e => e.FirstReminderSentAt)
               .HasColumnType("timestamptz");

        builder.Property(e => e.SecondReminderSentAt)
               .HasColumnType("timestamptz");

        builder.Property(e => e.FinalReminderSentAt)
               .HasColumnType("timestamptz");

        builder.Property(e => e.RespondedAt)
               .HasColumnType("timestamptz");

        builder.Property(e => e.FormExpiresAt)
               .HasColumnType("timestamptz");

        builder.Property(e => e.ManualContactAttemptAt)
               .HasColumnType("timestamptz");

        // Indexes
        builder.HasIndex(e => e.ApplicationId)
               .HasDatabaseName("IX_VettingReferences_ApplicationId");

        builder.HasIndex(e => e.ResponseToken)
               .IsUnique()
               .HasDatabaseName("IX_VettingReferences_ResponseToken");

        builder.HasIndex(e => e.Status)
               .HasDatabaseName("IX_VettingReferences_Status");

        builder.HasIndex(e => new { e.ApplicationId, e.ReferenceOrder })
               .IsUnique()
               .HasDatabaseName("IX_VettingReferences_ApplicationId_ReferenceOrder");

        // Performance index for reminder processing
        builder.HasIndex(e => new { e.Status, e.ContactedAt, e.FormExpiresAt })
               .HasDatabaseName("IX_VettingReferences_StatusProcessing")
               .HasFilter("\"Status\" IN (1, 2, 3)"); // NotContacted, Contacted, ReminderSent

        // Relationships
        builder.HasOne(e => e.Application)
               .WithMany(a => a.References)
               .HasForeignKey(e => e.ApplicationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Response)
               .WithOne(r => r.Reference)
               .HasForeignKey<VettingReferenceResponse>(r => r.ReferenceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.AuditLogs)
               .WithOne(a => a.Reference)
               .HasForeignKey(a => a.ReferenceId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}