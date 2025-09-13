using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.Vetting.Entities.Configuration;

/// <summary>
/// Entity Framework configuration for VettingDecisionAuditLog
/// </summary>
public class VettingDecisionAuditLogConfiguration : IEntityTypeConfiguration<VettingDecisionAuditLog>
{
    public void Configure(EntityTypeBuilder<VettingDecisionAuditLog> builder)
    {
        // Table mapping
        builder.ToTable("VettingDecisionAuditLog", "public");
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.ActionType)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(e => e.ActionDescription)
               .IsRequired()
               .HasColumnType("text");

        builder.Property(e => e.PreviousDecision)
               .HasColumnType("text");

        builder.Property(e => e.NewDecision)
               .HasColumnType("text");

        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        // Indexes
        builder.HasIndex(e => new { e.DecisionId, e.CreatedAt })
               .HasDatabaseName("IX_VettingDecisionAuditLog_DecisionId_CreatedAt");

        builder.HasIndex(e => e.ApplicationId)
               .HasDatabaseName("IX_VettingDecisionAuditLog_ApplicationId");

        builder.HasIndex(e => e.ActionType)
               .HasDatabaseName("IX_VettingDecisionAuditLog_ActionType");

        builder.HasIndex(e => e.UserId)
               .HasDatabaseName("IX_VettingDecisionAuditLog_UserId")
               .HasFilter("\"UserId\" IS NOT NULL");

        // Relationships
        builder.HasOne(e => e.Decision)
               .WithMany(d => d.AuditLogs)
               .HasForeignKey(e => e.DecisionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
               .WithMany()
               .HasForeignKey(e => e.UserId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}