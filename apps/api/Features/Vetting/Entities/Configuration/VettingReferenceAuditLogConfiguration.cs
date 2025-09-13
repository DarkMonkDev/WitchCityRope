using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.Vetting.Entities.Configuration;

/// <summary>
/// Entity Framework configuration for VettingReferenceAuditLog
/// </summary>
public class VettingReferenceAuditLogConfiguration : IEntityTypeConfiguration<VettingReferenceAuditLog>
{
    public void Configure(EntityTypeBuilder<VettingReferenceAuditLog> builder)
    {
        // Table mapping
        builder.ToTable("VettingReferenceAuditLog", "public");
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.ActionType)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(e => e.ActionDescription)
               .IsRequired()
               .HasColumnType("text");

        builder.Property(e => e.EmailMessageId)
               .HasMaxLength(200);

        builder.Property(e => e.DeliveryStatus)
               .HasMaxLength(50);

        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        // Indexes
        builder.HasIndex(e => new { e.ReferenceId, e.CreatedAt })
               .HasDatabaseName("IX_VettingReferenceAuditLog_ReferenceId_CreatedAt");

        builder.HasIndex(e => e.ApplicationId)
               .HasDatabaseName("IX_VettingReferenceAuditLog_ApplicationId");

        builder.HasIndex(e => e.ActionType)
               .HasDatabaseName("IX_VettingReferenceAuditLog_ActionType");

        builder.HasIndex(e => e.EmailMessageId)
               .HasDatabaseName("IX_VettingReferenceAuditLog_EmailMessageId")
               .HasFilter("\"EmailMessageId\" IS NOT NULL");

        // Relationships
        builder.HasOne(e => e.Reference)
               .WithMany(r => r.AuditLogs)
               .HasForeignKey(e => e.ReferenceId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}