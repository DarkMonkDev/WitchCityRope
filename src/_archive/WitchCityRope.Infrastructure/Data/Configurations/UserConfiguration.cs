using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.ValueObjects;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Id)
                .ValueGeneratedNever();

            builder.Property(u => u.EncryptedLegalName)
                .IsRequired()
                .HasMaxLength(500); // Encrypted data is longer than plain text

            // Configure SceneName value object
            builder.OwnsOne(u => u.SceneName, sceneName =>
            {
                sceneName.Property(sn => sn.Value)
                    .HasColumnName("SceneName")
                    .IsRequired()
                    .HasMaxLength(100);
                
                sceneName.HasIndex(sn => sn.Value)
                    .IsUnique();
            });

            // Configure EmailAddress value object
            builder.OwnsOne(u => u.Email, email =>
            {
                email.Property(e => e.Value)
                    .HasColumnName("Email")
                    .IsRequired()
                    .HasMaxLength(256);
                
                email.HasIndex(e => e.Value)
                    .IsUnique();
            });

            builder.Property(u => u.DateOfBirth)
                .IsRequired();

            builder.Property(u => u.Role)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(u => u.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(u => u.CreatedAt)
                .IsRequired();

            builder.Property(u => u.UpdatedAt)
                .IsRequired();
                
            builder.Property(u => u.PronouncedName)
                .HasMaxLength(100);
                
            builder.Property(u => u.Pronouns)
                .HasMaxLength(50);
                
            builder.Property(u => u.IsVetted)
                .IsRequired()
                .HasDefaultValue(false);
                
            builder.Ignore(u => u.DisplayName); // Computed property

            // Configure relationships
            builder.HasMany(u => u.Registrations)
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.VettingApplications)
                .WithOne(v => v.Applicant)
                .HasForeignKey(v => v.ApplicantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(u => u.IsActive);
            builder.HasIndex(u => u.CreatedAt);
            builder.HasIndex(u => u.IsVetted);
        }
    }
}