using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.CheckIn.Entities.Configuration;

/// <summary>
/// Entity Framework Core configuration for CheckInSessionToken
/// Defines database schema, constraints, and indexes
/// </summary>
public class CheckInSessionTokenConfiguration : IEntityTypeConfiguration<CheckInSessionToken>
{
    public void Configure(EntityTypeBuilder<CheckInSessionToken> builder)
    {
        // Table mapping
        builder.ToTable("CheckInSessionTokens", "public");

        // NOTE: Removed CHK_CheckInSessionTokens_SessionBelongsToEvent check constraint
        // PostgreSQL does NOT support subqueries (SELECT, EXISTS) in check constraints
        // Data integrity enforced by FK_CheckInSessionTokens_Sessions_SessionId foreign key instead

        builder.HasKey(t => t.Id);

        // Token: Required, 88 chars (64 bytes base64-encoded), unique
        builder.Property(t => t.Token)
            .IsRequired()
            .HasMaxLength(88);

        builder.HasIndex(t => t.Token)
            .IsUnique()
            .HasDatabaseName("IX_CheckInSessionTokens_Token");

        // EventId: Required, indexed for lookups
        builder.Property(t => t.EventId)
            .IsRequired();

        builder.HasIndex(t => t.EventId)
            .HasDatabaseName("IX_CheckInSessionTokens_EventId");

        // SessionId: Required, indexed for lookups
        builder.Property(t => t.SessionId)
            .IsRequired();

        builder.HasIndex(t => t.SessionId)
            .HasDatabaseName("IX_CheckInSessionTokens_SessionId");

        // CreatedByUserId: Required for audit trail
        builder.Property(t => t.CreatedByUserId)
            .IsRequired();

        builder.HasIndex(t => t.CreatedByUserId)
            .HasDatabaseName("IX_CheckInSessionTokens_CreatedByUserId");

        // DateTime properties: CRITICAL - Use timestamptz for PostgreSQL
        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(t => t.ExpiresAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.Property(t => t.RevokedAt)
            .HasColumnType("timestamptz");

        builder.Property(t => t.LastUsedAt)
            .HasColumnType("timestamptz");

        // IsRevoked: Required, default false
        builder.Property(t => t.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);

        // Partial index for active tokens only (performance optimization)
        builder.HasIndex(t => new { t.SessionId, t.ExpiresAt })
            .HasDatabaseName("IX_CheckInSessionTokens_SessionId_ExpiresAt_Active")
            .HasFilter("\"IsRevoked\" = false");

        // Composite index for token validation queries
        builder.HasIndex(t => new { t.Token, t.ExpiresAt, t.IsRevoked })
            .HasDatabaseName("IX_CheckInSessionTokens_Token_ExpiresAt_IsRevoked");
    }
}
