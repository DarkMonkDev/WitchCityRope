using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Api.Features.Payments.Entities;

namespace WitchCityRope.Api.Features.Payments.Data;

/// <summary>
/// Entity Framework configuration for PaymentAuditLog entity
/// Comprehensive audit trail for compliance and troubleshooting
/// </summary>
public class PaymentAuditLogConfiguration : IEntityTypeConfiguration<PaymentAuditLog>
{
    public void Configure(EntityTypeBuilder<PaymentAuditLog> builder)
    {
        // Table configuration with check constraints
        builder.ToTable("PaymentAuditLog", "public", t =>
        {
            // Action type validation
            t.HasCheckConstraint("CHK_PaymentAuditLog_ActionType",
                "\"ActionType\" IN ('PaymentInitiated', 'PaymentProcessed', 'PaymentCompleted', 'PaymentFailed', 'PaymentRetried', 'RefundInitiated', 'RefundCompleted', 'RefundFailed', 'StatusChanged', 'MetadataUpdated', 'SystemAction')");
        });

        builder.HasKey(a => a.Id);

        // ID initialization
        builder.Property(a => a.Id)
               .ValueGeneratedOnAdd();

        // Required fields
        builder.Property(a => a.PaymentId)
               .IsRequired();

        #region Action Details

        builder.Property(a => a.ActionType)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(a => a.ActionDescription)
               .IsRequired()
               .HasColumnType("text");

        #endregion

        #region Change Tracking (JSONB)

        builder.Property(a => a.OldValues)
               .HasConversion(
                   v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                   v => v == null ? new Dictionary<string, object>() :
                        System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
               );

        builder.Property(a => a.NewValues)
               .HasConversion(
                   v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                   v => v == null ? new Dictionary<string, object>() :
                        System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
               );

        // PostgreSQL-specific configuration (skip for InMemory)
        try
        {
            builder.Property(a => a.OldValues)
                   .HasColumnType("jsonb");
            builder.Property(a => a.NewValues)
                   .HasColumnType("jsonb");
        }
        catch
        {
            // Skip column type configuration for InMemory database
        }

        #endregion

        #region Security Tracking

        builder.Property(a => a.IpAddress)
               .HasMaxLength(45); // IPv6 support

        builder.Property(a => a.UserAgent)
               .HasMaxLength(1000); // Modern user agents can be long

        #endregion

        #region DateTime Configuration

        builder.Property(a => a.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz")
               .HasDefaultValueSql("NOW()");

        #endregion

        #region Foreign Key Relationships

        builder.HasOne(a => a.Payment)
               .WithMany(p => p.AuditLogs)
               .HasForeignKey(a => a.PaymentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.User)
               .WithMany()
               .HasForeignKey(a => a.UserId)
               .OnDelete(DeleteBehavior.SetNull);

        #endregion

        #region Performance Indexes

        // Composite index for most common audit queries
        builder.HasIndex(a => new { a.PaymentId, a.CreatedAt })
               .HasDatabaseName("IX_PaymentAuditLog_PaymentId_CreatedAt");

        builder.HasIndex(a => new { a.UserId, a.CreatedAt })
               .HasDatabaseName("IX_PaymentAuditLog_UserId_CreatedAt");

        builder.HasIndex(a => a.ActionType)
               .HasDatabaseName("IX_PaymentAuditLog_ActionType");

        builder.HasIndex(a => a.CreatedAt)
               .HasDatabaseName("IX_PaymentAuditLog_CreatedAt");

        // GIN indexes for JSONB columns (efficient for complex queries)
        builder.HasIndex(a => a.OldValues)
               .HasDatabaseName("IX_PaymentAuditLog_OldValues_Gin")
               .HasMethod("gin");

        builder.HasIndex(a => a.NewValues)
               .HasDatabaseName("IX_PaymentAuditLog_NewValues_Gin")
               .HasMethod("gin");

        // Partial index for failed operations (troubleshooting focus)
        builder.HasIndex(a => a.CreatedAt)
               .HasDatabaseName("IX_PaymentAuditLog_FailedActions")
               .HasFilter("\"ActionType\" IN ('PaymentFailed', 'RefundFailed')");

        #endregion
    }
}