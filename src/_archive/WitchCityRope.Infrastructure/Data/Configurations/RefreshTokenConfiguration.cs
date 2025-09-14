using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Token)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(e => e.ReplacedByToken)
            .HasMaxLength(512);

        builder.HasIndex(e => e.Token)
            .IsUnique();

        builder.HasIndex(e => e.UserId);

        builder.HasIndex(e => e.ExpiresAt);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}