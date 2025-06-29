using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Data.Configurations;

public class UserAuthenticationConfiguration : IEntityTypeConfiguration<UserAuthentication>
{
    public void Configure(EntityTypeBuilder<UserAuthentication> builder)
    {
        builder.ToTable("UserAuthentications");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.PasswordHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.TwoFactorSecret)
            .HasMaxLength(256);

        builder.HasIndex(e => e.UserId)
            .IsUnique();

        builder.HasOne(e => e.User)
            .WithOne()
            .HasForeignKey<UserAuthentication>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}