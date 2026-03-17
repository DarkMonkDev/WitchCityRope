using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.Authentication.Entities.Configuration;

/// <summary>
/// Entity Framework Core configuration for RefreshToken
/// Defines database schema, constraints, and indexes for persistent login tokens
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        // Table mapping
        builder.ToTable("RefreshTokens", "public");

        builder.HasKey(t => t.Id);

        // Token: Required, max 128 chars (64 bytes base64-encoded ~88 chars), unique
        builder.Property(t => t.Token)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(t => t.Token)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_Token");

        // UserId: Required, indexed for lookups
        builder.Property(t => t.UserId)
            .IsRequired();

        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");

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

        // ReplacedByToken: nullable, max 128 chars
        builder.Property(t => t.ReplacedByToken)
            .HasMaxLength(128);

        // DeviceInfo: nullable, max 256 chars
        builder.Property(t => t.DeviceInfo)
            .HasMaxLength(256);

        // Partial index for active tokens per user (performance optimization)
        builder.HasIndex(t => new { t.UserId, t.ExpiresAt })
            .HasDatabaseName("IX_RefreshTokens_UserId_ExpiresAt_Active")
            .HasFilter("\"IsRevoked\" = false");

        // Composite index for token validation queries
        builder.HasIndex(t => new { t.Token, t.ExpiresAt, t.IsRevoked })
            .HasDatabaseName("IX_RefreshTokens_Token_ExpiresAt_IsRevoked");

        // Foreign key to ApplicationUser with CASCADE delete
        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
