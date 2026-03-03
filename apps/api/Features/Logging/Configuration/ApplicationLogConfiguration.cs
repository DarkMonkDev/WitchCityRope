using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Api.Features.Logging.Entities;

namespace WitchCityRope.Api.Features.Logging.Configuration;

/// <summary>
/// EF Core configuration for ApplicationLog entity.
/// This table is written to by Serilog's PostgreSQL sink, not by EF Core.
/// EF Core maps it as read-only for admin query purposes.
/// </summary>
public class ApplicationLogConfiguration : IEntityTypeConfiguration<ApplicationLog>
{
    public void Configure(EntityTypeBuilder<ApplicationLog> builder)
    {
        builder.ToTable("application_logs", "logging");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
               .HasColumnName("id")
               .UseIdentityAlwaysColumn();

        builder.Property(l => l.Timestamp)
               .HasColumnName("timestamp")
               .HasColumnType("timestamptz")
               .IsRequired();

        builder.Property(l => l.Level)
               .HasColumnName("level")
               .IsRequired();

        builder.Property(l => l.LevelName)
               .HasColumnName("level_name")
               .HasMaxLength(16)
               .IsRequired();

        builder.Property(l => l.Message)
               .HasColumnName("message")
               .HasColumnType("text")
               .IsRequired();

        builder.Property(l => l.MessageTemplate)
               .HasColumnName("message_template")
               .HasColumnType("text");

        builder.Property(l => l.Exception)
               .HasColumnName("exception")
               .HasColumnType("text");

        builder.Property(l => l.SourceContext)
               .HasColumnName("source_context")
               .HasMaxLength(512);

        builder.Property(l => l.Properties)
               .HasColumnName("properties")
               .HasColumnType("jsonb");

        builder.Property(l => l.UserId)
               .HasColumnName("user_id");

        builder.Property(l => l.CorrelationId)
               .HasColumnName("correlation_id");

        builder.Property(l => l.RequestPath)
               .HasColumnName("request_path")
               .HasMaxLength(2048);

        builder.Property(l => l.MachineName)
               .HasColumnName("machine_name")
               .HasMaxLength(256);

        // BRIN index on timestamp - optimal for append-only time-series data
        builder.HasIndex(l => l.Timestamp)
               .HasDatabaseName("IX_application_logs_timestamp")
               .HasMethod("brin");

        // B-tree on level for filtering by severity
        builder.HasIndex(l => l.Level)
               .HasDatabaseName("IX_application_logs_level");

        // Partial index for recent errors (level >= 3 = Warning+)
        builder.HasIndex(l => new { l.Timestamp, l.SourceContext })
               .HasDatabaseName("IX_application_logs_recent_errors")
               .HasFilter("level >= 3");

        // Partial index on user_id for user-specific log queries
        builder.HasIndex(l => l.UserId)
               .HasDatabaseName("IX_application_logs_user_id")
               .HasFilter("user_id IS NOT NULL");

        // Partial index on correlation_id for request tracing
        builder.HasIndex(l => l.CorrelationId)
               .HasDatabaseName("IX_application_logs_correlation_id")
               .HasFilter("correlation_id IS NOT NULL");

        // GIN index on JSONB properties for flexible querying
        builder.HasIndex(l => l.Properties)
               .HasDatabaseName("IX_application_logs_properties_gin")
               .HasMethod("gin")
               .HasOperators("jsonb_path_ops");
    }
}
