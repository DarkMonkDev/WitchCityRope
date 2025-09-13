using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.Vetting.Entities.Configuration;

/// <summary>
/// Entity Framework configuration for VettingReviewer
/// </summary>
public class VettingReviewerConfiguration : IEntityTypeConfiguration<VettingReviewer>
{
    public void Configure(EntityTypeBuilder<VettingReviewer> builder)
    {
        // Table mapping
        builder.ToTable("VettingReviewers", "public");
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.SpecializationsJson)
               .IsRequired()
               .HasColumnType("jsonb")
               .HasDefaultValue("[]");

        builder.Property(e => e.WorkingHoursJson)
               .HasColumnType("jsonb");

        builder.Property(e => e.TimeZone)
               .IsRequired()
               .HasMaxLength(50)
               .HasDefaultValue("UTC");

        // Performance metrics
        builder.Property(e => e.AverageReviewTimeHours)
               .HasColumnType("decimal(10,2)");

        builder.Property(e => e.ApprovalRate)
               .HasColumnType("decimal(5,2)");

        // DateTime properties
        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.UpdatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.LastActivityAt)
               .HasColumnType("timestamptz");

        builder.Property(e => e.UnavailableUntil)
               .HasColumnType("timestamptz");

        // Indexes
        builder.HasIndex(e => e.UserId)
               .IsUnique()
               .HasDatabaseName("IX_VettingReviewers_UserId");

        builder.HasIndex(e => e.IsActive)
               .HasDatabaseName("IX_VettingReviewers_IsActive");

        builder.HasIndex(e => new { e.IsActive, e.IsAvailable, e.CurrentWorkload, e.MaxWorkload })
               .HasDatabaseName("IX_VettingReviewers_Workload")
               .HasFilter("\"IsActive\" = true");

        // GIN index for specializations search
        builder.HasIndex(e => e.SpecializationsJson)
               .HasDatabaseName("IX_VettingReviewers_Specializations")
               .HasMethod("gin");

        // Relationships
        builder.HasOne(e => e.User)
               .WithMany()
               .HasForeignKey(e => e.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.AssignedApplications)
               .WithOne(a => a.AssignedReviewer)
               .HasForeignKey(a => a.AssignedReviewerId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Decisions)
               .WithOne(d => d.Reviewer)
               .HasForeignKey(d => d.ReviewerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Notes)
               .WithOne(n => n.Reviewer)
               .HasForeignKey(n => n.ReviewerId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}