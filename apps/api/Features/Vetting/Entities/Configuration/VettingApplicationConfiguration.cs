using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.Vetting.Entities.Configuration;

/// <summary>
/// Entity Framework configuration for simplified VettingApplication
/// Aligned with design specifications and wireframe requirements
/// </summary>
public class VettingApplicationConfiguration : IEntityTypeConfiguration<VettingApplication>
{
    public void Configure(EntityTypeBuilder<VettingApplication> builder)
    {
        builder.ToTable("VettingApplications");

        builder.HasKey(x => x.Id);

        // Required string fields with reasonable limits
        builder.Property(x => x.SceneName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.RealName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.AboutYourself)
            .IsRequired()
            .HasMaxLength(2000);

        // Optional fields
        builder.Property(x => x.FetLifeHandle)
            .HasMaxLength(100);

        builder.Property(x => x.Pronouns)
            .HasMaxLength(50);

        builder.Property(x => x.OtherNames)
            .HasMaxLength(200);

        builder.Property(x => x.AdminNotes)
            .HasMaxLength(4000);

        // Workflow status enum
        builder.Property(x => x.WorkflowStatus)
            .IsRequired()
            .HasConversion<int>();

        // DateTime with PostgreSQL timestamptz
        builder.Property(x => x.SubmittedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        // Foreign key relationship to User
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-many relationship with audit logs
        builder.HasMany(x => x.AuditLogs)
            .WithOne(a => a.Application)
            .HasForeignKey(a => a.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(x => x.UserId)
            .IsUnique() // One application per user
            .HasDatabaseName("IX_VettingApplications_UserId");

        builder.HasIndex(x => x.WorkflowStatus)
            .HasDatabaseName("IX_VettingApplications_WorkflowStatus");

        builder.HasIndex(x => x.SubmittedAt)
            .HasDatabaseName("IX_VettingApplications_SubmittedAt");

        builder.HasIndex(x => x.Email)
            .HasDatabaseName("IX_VettingApplications_Email");
    }
}