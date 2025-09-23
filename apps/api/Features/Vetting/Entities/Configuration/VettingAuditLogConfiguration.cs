using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.Vetting.Entities.Configuration;

/// <summary>
/// Entity Framework configuration for VettingAuditLog
/// Simple audit log configuration replacing complex audit system
/// </summary>
public class VettingAuditLogConfiguration : IEntityTypeConfiguration<VettingAuditLog>
{
    public void Configure(EntityTypeBuilder<VettingAuditLog> builder)
    {
        builder.ToTable("VettingAuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.OldValue)
            .HasMaxLength(4000);

        builder.Property(x => x.NewValue)
            .HasMaxLength(4000);

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.Property(x => x.PerformedAt)
            .IsRequired();

        // Foreign key relationships
        builder.HasOne(x => x.Application)
            .WithMany(a => a.AuditLogs)
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PerformedByUser)
            .WithMany()
            .HasForeignKey(x => x.PerformedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for performance
        builder.HasIndex(x => x.ApplicationId);
        builder.HasIndex(x => x.PerformedAt);
        builder.HasIndex(x => x.Action);
    }
}