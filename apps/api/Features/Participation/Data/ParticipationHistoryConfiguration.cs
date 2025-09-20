using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Api.Features.Participation.Entities;

namespace WitchCityRope.Api.Features.Participation.Data;

/// <summary>
/// Entity Framework configuration for ParticipationHistory entity
/// Implements comprehensive audit trail patterns
/// </summary>
public class ParticipationHistoryConfiguration : IEntityTypeConfiguration<ParticipationHistory>
{
    public void Configure(EntityTypeBuilder<ParticipationHistory> builder)
    {
        // Table mapping
        builder.ToTable("ParticipationHistory", "public");
        builder.HasKey(h => h.Id);

        // Property configurations
        builder.Property(h => h.Id)
               .ValueGeneratedOnAdd();

        builder.Property(h => h.ParticipationId)
               .IsRequired();

        builder.Property(h => h.ActionType)
               .IsRequired()
               .HasMaxLength(50);

        // JSONB for old/new values
        builder.Property(h => h.OldValues)
               .HasColumnType("jsonb");

        builder.Property(h => h.NewValues)
               .HasColumnType("jsonb");

        builder.Property(h => h.ChangeReason)
               .HasMaxLength(1000);

        builder.Property(h => h.IpAddress)
               .HasMaxLength(45); // IPv6 compatible

        builder.Property(h => h.UserAgent)
               .HasMaxLength(500);

        // UTC DateTime handling
        builder.Property(h => h.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        // Foreign key relationships
        builder.HasOne(h => h.Participation)
               .WithMany(e => e.History)
               .HasForeignKey(h => h.ParticipationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.ChangedByUser)
               .WithMany()
               .HasForeignKey(h => h.ChangedBy)
               .OnDelete(DeleteBehavior.SetNull);

        // Indexes for performance
        builder.HasIndex(h => new { h.ParticipationId, h.CreatedAt })
               .HasDatabaseName("IX_ParticipationHistory_ParticipationId_CreatedAt");

        builder.HasIndex(h => h.ActionType)
               .HasDatabaseName("IX_ParticipationHistory_ActionType");

        // GIN indexes for JSONB queries
        builder.HasIndex(h => h.OldValues)
               .HasDatabaseName("IX_ParticipationHistory_OldValues_Gin")
               .HasMethod("gin");

        builder.HasIndex(h => h.NewValues)
               .HasDatabaseName("IX_ParticipationHistory_NewValues_Gin")
               .HasMethod("gin");

        // Business rule constraints
        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_ParticipationHistory_ActionType",
            "\"ActionType\" IN ('Created', 'Updated', 'Cancelled', 'Refunded', 'StatusChanged', 'PaymentUpdated')"));
    }
}