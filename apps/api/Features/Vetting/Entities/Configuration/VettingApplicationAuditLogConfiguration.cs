using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.Vetting.Entities.Configuration;

/// <summary>
/// Entity Framework configuration for VettingApplicationAuditLog
/// </summary>
public class VettingApplicationAuditLogConfiguration : IEntityTypeConfiguration<VettingApplicationAuditLog>
{
    public void Configure(EntityTypeBuilder<VettingApplicationAuditLog> builder)
    {
        // Table mapping
        builder.ToTable("VettingApplicationAuditLog", "public");
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.ActionType)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(e => e.ActionDescription)
               .IsRequired()
               .HasColumnType("text");

        builder.Property(e => e.OldValues)
               .HasColumnType("jsonb");

        builder.Property(e => e.NewValues)
               .HasColumnType("jsonb");

        builder.Property(e => e.IpAddress)
               .HasMaxLength(45);

        builder.Property(e => e.UserAgent)
               .HasMaxLength(500);

        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        // Indexes
        builder.HasIndex(e => new { e.ApplicationId, e.CreatedAt })
               .HasDatabaseName("IX_VettingApplicationAuditLog_ApplicationId_CreatedAt");

        builder.HasIndex(e => e.ActionType)
               .HasDatabaseName("IX_VettingApplicationAuditLog_ActionType");

        builder.HasIndex(e => e.CreatedAt)
               .HasDatabaseName("IX_VettingApplicationAuditLog_CreatedAt");

        // GIN indexes for JSONB columns
        builder.HasIndex(e => e.OldValues)
               .HasDatabaseName("IX_VettingApplicationAuditLog_OldValues")
               .HasMethod("gin");

        builder.HasIndex(e => e.NewValues)
               .HasDatabaseName("IX_VettingApplicationAuditLog_NewValues")
               .HasMethod("gin");

        // Relationships
        builder.HasOne(e => e.Application)
               .WithMany(a => a.AuditLogs)
               .HasForeignKey(e => e.ApplicationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
               .WithMany()
               .HasForeignKey(e => e.UserId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}