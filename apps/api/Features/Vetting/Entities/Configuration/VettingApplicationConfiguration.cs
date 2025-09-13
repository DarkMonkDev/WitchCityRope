using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.Vetting.Entities.Configuration;

/// <summary>
/// Entity Framework configuration for VettingApplication
/// Follows WitchCityRope patterns with PostgreSQL optimizations
/// </summary>
public class VettingApplicationConfiguration : IEntityTypeConfiguration<VettingApplication>
{
    public void Configure(EntityTypeBuilder<VettingApplication> builder)
    {
        // Table mapping
        builder.ToTable("VettingApplications", "public");
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.ApplicationNumber)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(e => e.StatusToken)
               .IsRequired()
               .HasMaxLength(100);

        // Encrypted fields - larger size to accommodate encrypted data
        builder.Property(e => e.EncryptedFullName)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(e => e.EncryptedSceneName)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(e => e.EncryptedPronouns)
               .HasMaxLength(200);

        builder.Property(e => e.EncryptedEmail)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(e => e.EncryptedPhone)
               .HasMaxLength(200);

        builder.Property(e => e.EncryptedExperienceDescription)
               .IsRequired()
               .HasColumnType("text");

        builder.Property(e => e.EncryptedSafetyKnowledge)
               .IsRequired()
               .HasColumnType("text");

        builder.Property(e => e.EncryptedConsentUnderstanding)
               .IsRequired()
               .HasColumnType("text");

        builder.Property(e => e.EncryptedWhyJoinCommunity)
               .IsRequired()
               .HasColumnType("text");

        builder.Property(e => e.EncryptedExpectationsGoals)
               .IsRequired()
               .HasColumnType("text");

        // JSON field for skills/interests - using PostgreSQL JSONB
        builder.Property(e => e.SkillsInterests)
               .IsRequired()
               .HasColumnType("jsonb")
               .HasDefaultValue("[]");

        // Enum configurations
        builder.Property(e => e.Status)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(e => e.ExperienceLevel)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(e => e.Priority)
               .IsRequired()
               .HasConversion<int>();

        // DateTime properties - CRITICAL: Use timestamptz for PostgreSQL
        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.UpdatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.ReviewStartedAt)
               .HasColumnType("timestamptz");

        builder.Property(e => e.DecisionMadeAt)
               .HasColumnType("timestamptz");

        builder.Property(e => e.InterviewScheduledFor)
               .HasColumnType("timestamptz");

        builder.Property(e => e.DeletedAt)
               .HasColumnType("timestamptz");

        builder.Property(e => e.ExpiresAt)
               .HasColumnType("timestamptz");

        // Indexes for performance
        builder.HasIndex(e => e.ApplicationNumber)
               .IsUnique()
               .HasDatabaseName("IX_VettingApplications_ApplicationNumber");

        builder.HasIndex(e => e.StatusToken)
               .IsUnique()
               .HasDatabaseName("IX_VettingApplications_StatusToken");

        builder.HasIndex(e => e.Status)
               .HasDatabaseName("IX_VettingApplications_Status");

        builder.HasIndex(e => e.AssignedReviewerId)
               .HasDatabaseName("IX_VettingApplications_AssignedReviewerId")
               .HasFilter("\"AssignedReviewerId\" IS NOT NULL");

        builder.HasIndex(e => new { e.Status, e.Priority, e.CreatedAt })
               .HasDatabaseName("IX_VettingApplications_Status_Priority_CreatedAt");

        builder.HasIndex(e => e.DeletedAt)
               .HasDatabaseName("IX_VettingApplications_DeletedAt")
               .HasFilter("\"DeletedAt\" IS NOT NULL");

        // Partial index for active applications
        builder.HasIndex(e => new { e.Status, e.CreatedAt })
               .HasDatabaseName("IX_VettingApplications_Active_Status_CreatedAt")
               .HasFilter("\"DeletedAt\" IS NULL");

        // GIN index for JSONB skills/interests search
        builder.HasIndex(e => e.SkillsInterests)
               .HasDatabaseName("IX_VettingApplications_SkillsInterests")
               .HasMethod("gin");

        // Relationships
        builder.HasOne(e => e.Applicant)
               .WithMany()
               .HasForeignKey(e => e.ApplicantId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.AssignedReviewer)
               .WithMany(r => r.AssignedApplications)
               .HasForeignKey(e => e.AssignedReviewerId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.CreatedByUser)
               .WithMany()
               .HasForeignKey(e => e.CreatedBy)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.UpdatedByUser)
               .WithMany()
               .HasForeignKey(e => e.UpdatedBy)
               .OnDelete(DeleteBehavior.SetNull);

        // One-to-many relationships
        builder.HasMany(e => e.References)
               .WithOne(r => r.Application)
               .HasForeignKey(r => r.ApplicationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Notes)
               .WithOne(n => n.Application)
               .HasForeignKey(n => n.ApplicationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Decisions)
               .WithOne(d => d.Application)
               .HasForeignKey(d => d.ApplicationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.AuditLogs)
               .WithOne(a => a.Application)
               .HasForeignKey(a => a.ApplicationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Notifications)
               .WithOne(n => n.Application)
               .HasForeignKey(n => n.ApplicationId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}