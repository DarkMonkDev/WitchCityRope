using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    public class UserNoteConfiguration : IEntityTypeConfiguration<UserNote>
    {
        public void Configure(EntityTypeBuilder<UserNote> builder)
        {
            builder.ToTable("UserNotes");

            builder.HasKey(n => n.Id);

            builder.Property(n => n.Id)
                .ValueGeneratedNever();

            builder.Property(n => n.NoteType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(n => n.Content)
                .IsRequired()
                .HasMaxLength(5000);

            builder.Property(n => n.CreatedAt)
                .IsRequired();

            builder.Property(n => n.UpdatedAt)
                .IsRequired();

            builder.Property(n => n.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            // Relationships - navigation properties only exist at Infrastructure level
            builder.HasOne<WitchCityRopeUser>()
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasOne<WitchCityRopeUser>()
                .WithMany()
                .HasForeignKey(n => n.CreatedById)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasOne<WitchCityRopeUser>()
                .WithMany()
                .HasForeignKey(n => n.DeletedById)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // Global query filter to exclude soft-deleted notes
            builder.HasQueryFilter(n => !n.IsDeleted);

            // Indexes
            builder.HasIndex(n => n.UserId)
                .HasDatabaseName("IX_UserNotes_UserId");

            builder.HasIndex(n => n.NoteType)
                .HasDatabaseName("IX_UserNotes_NoteType");

            builder.HasIndex(n => n.CreatedAt)
                .HasDatabaseName("IX_UserNotes_CreatedAt")
                .IsDescending();

            builder.HasIndex(n => n.CreatedById)
                .HasDatabaseName("IX_UserNotes_CreatedById");

            builder.HasIndex(n => new { n.UserId, n.IsDeleted })
                .HasDatabaseName("IX_UserNotes_UserId_IsDeleted");
        }
    }
}