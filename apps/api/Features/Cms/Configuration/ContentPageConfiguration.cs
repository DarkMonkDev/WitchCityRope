using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Api.Features.Cms.Entities;

namespace WitchCityRope.Api.Features.Cms.Configuration
{
    public class ContentPageConfiguration : IEntityTypeConfiguration<ContentPage>
    {
        public void Configure(EntityTypeBuilder<ContentPage> builder)
        {
            // Table configuration
            builder.ToTable("ContentPages", "public");

            // Primary key
            builder.HasKey(cp => cp.Id);
            builder.Property(cp => cp.Id)
                .HasColumnName("Id")
                .UseIdentityColumn();

            // Slug configuration
            builder.Property(cp => cp.Slug)
                .HasColumnName("Slug")
                .HasMaxLength(100)
                .IsRequired();

            builder.HasIndex(cp => cp.Slug)
                .IsUnique()
                .HasDatabaseName("UX_ContentPages_Slug");

            // Title configuration
            builder.Property(cp => cp.Title)
                .HasColumnName("Title")
                .HasMaxLength(200)
                .IsRequired();

            // Content configuration (PostgreSQL TEXT type)
            builder.Property(cp => cp.Content)
                .HasColumnName("Content")
                .HasColumnType("TEXT")
                .IsRequired();

            // Audit fields configuration (UTC DateTime)
            builder.Property(cp => cp.CreatedAt)
                .HasColumnName("CreatedAt")
                .HasColumnType("timestamp with time zone")
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            builder.Property(cp => cp.UpdatedAt)
                .HasColumnName("UpdatedAt")
                .HasColumnType("timestamp with time zone")
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            // User attribution fields
            builder.Property(cp => cp.CreatedBy)
                .HasColumnName("CreatedBy")
                .IsRequired();

            builder.Property(cp => cp.LastModifiedBy)
                .HasColumnName("LastModifiedBy")
                .IsRequired();

            // Published state
            builder.Property(cp => cp.IsPublished)
                .HasColumnName("IsPublished")
                .IsRequired()
                .HasDefaultValue(true);

            // Foreign key relationships
            builder.HasOne(cp => cp.CreatedByUser)
                .WithMany()
                .HasForeignKey(cp => cp.CreatedBy)
                .HasConstraintName("FK_ContentPages_CreatedBy")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(cp => cp.LastModifiedByUser)
                .WithMany()
                .HasForeignKey(cp => cp.LastModifiedBy)
                .HasConstraintName("FK_ContentPages_LastModifiedBy")
                .OnDelete(DeleteBehavior.Restrict);

            // Revisions relationship
            builder.HasMany(cp => cp.Revisions)
                .WithOne(r => r.ContentPage)
                .HasForeignKey(r => r.ContentPageId)
                .HasConstraintName("FK_ContentRevisions_ContentPage")
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(cp => cp.IsPublished)
                .HasDatabaseName("IX_ContentPages_IsPublished")
                .HasFilter("\"IsPublished\" = true");

            builder.HasIndex(cp => cp.CreatedBy)
                .HasDatabaseName("IX_ContentPages_CreatedBy");

            builder.HasIndex(cp => cp.LastModifiedBy)
                .HasDatabaseName("IX_ContentPages_LastModifiedBy");

            builder.HasIndex(cp => cp.UpdatedAt)
                .HasDatabaseName("IX_ContentPages_UpdatedAt")
                .IsDescending();

            // Check constraints
            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CHK_ContentPages_Slug_Format",
                    "\"Slug\" ~ '^[a-z0-9]+(-[a-z0-9]+)*$'"
                );

                t.HasCheckConstraint(
                    "CHK_ContentPages_Title_Length",
                    "LENGTH(TRIM(\"Title\")) >= 3"
                );

                t.HasCheckConstraint(
                    "CHK_ContentPages_Content_NotEmpty",
                    "LENGTH(TRIM(\"Content\")) > 0"
                );
            });
        }
    }
}
