using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.Vetting.Entities.Configuration;

/// <summary>
/// Entity Framework configuration for VettingDecision
/// </summary>
public class VettingDecisionConfiguration : IEntityTypeConfiguration<VettingDecision>
{
    public void Configure(EntityTypeBuilder<VettingDecision> builder)
    {
        // Table mapping
        builder.ToTable("VettingDecisions", "public");
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.DecisionType)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(e => e.Reasoning)
               .IsRequired()
               .HasColumnType("text");

        builder.Property(e => e.AdditionalInfoRequested)
               .HasColumnType("text");

        builder.Property(e => e.InterviewNotes)
               .HasColumnType("text");

        builder.Property(e => e.DecisionIpAddress)
               .HasMaxLength(45);

        builder.Property(e => e.DecisionUserAgent)
               .HasMaxLength(500);

        // DateTime properties
        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.AdditionalInfoDeadline)
               .HasColumnType("timestamptz");

        builder.Property(e => e.ProposedInterviewTime)
               .HasColumnType("timestamptz");

        // Indexes
        builder.HasIndex(e => e.ApplicationId)
               .HasDatabaseName("IX_VettingDecisions_ApplicationId");

        builder.HasIndex(e => e.ReviewerId)
               .HasDatabaseName("IX_VettingDecisions_ReviewerId");

        builder.HasIndex(e => new { e.ApplicationId, e.CreatedAt })
               .HasDatabaseName("IX_VettingDecisions_ApplicationId_CreatedAt");

        builder.HasIndex(e => e.DecisionType)
               .HasDatabaseName("IX_VettingDecisions_DecisionType");

        builder.HasIndex(e => e.IsFinalDecision)
               .HasDatabaseName("IX_VettingDecisions_IsFinalDecision")
               .HasFilter("\"IsFinalDecision\" = true");

        // Relationships
        builder.HasOne(e => e.Application)
               .WithMany(a => a.Decisions)
               .HasForeignKey(e => e.ApplicationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Reviewer)
               .WithMany(r => r.Decisions)
               .HasForeignKey(e => e.ReviewerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.AuditLogs)
               .WithOne(a => a.Decision)
               .HasForeignKey(a => a.DecisionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}