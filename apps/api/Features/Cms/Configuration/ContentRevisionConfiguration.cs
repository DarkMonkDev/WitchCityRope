using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Api.Features.Cms.Entities;

namespace WitchCityRope.Api.Features.Cms.Configuration
{
    public class ContentRevisionConfiguration : IEntityTypeConfiguration<ContentRevision>
    {
        public void Configure(EntityTypeBuilder<ContentRevision> builder)
        {
            // Table configuration
            builder.ToTable("ContentRevisions", "public");

            // Primary key
            builder.HasKey(cr => cr.Id);
            builder.Property(cr => cr.Id)
                .HasColumnName("Id")
                .UseIdentityColumn();

            // Foreign key to ContentPage
            builder.Property(cr => cr.ContentPageId)
                .HasColumnName("ContentPageId")
                .IsRequired();

            // Content snapshot (PostgreSQL TEXT type)
            builder.Property(cr => cr.Content)
                .HasColumnName("Content")
                .HasColumnType("TEXT")
                .IsRequired();

            // Title snapshot
            builder.Property(cr => cr.Title)
                .HasColumnName("Title")
                .HasMaxLength(200)
                .IsRequired();

            // Audit fields (UTC DateTime)
            builder.Property(cr => cr.CreatedAt)
                .HasColumnName("CreatedAt")
                .HasColumnType("timestamp with time zone")
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            builder.Property(cr => cr.CreatedBy)
                .HasColumnName("CreatedBy")
                .IsRequired();

            // Optional change description
            builder.Property(cr => cr.ChangeDescription)
                .HasColumnName("ChangeDescription")
                .HasMaxLength(500)
                .IsRequired(false);

            // Foreign key relationships
            builder.HasOne(cr => cr.ContentPage)
                .WithMany(cp => cp.Revisions)
                .HasForeignKey(cr => cr.ContentPageId)
                .HasConstraintName("FK_ContentRevisions_ContentPage")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(cr => cr.CreatedByUser)
                .WithMany()
                .HasForeignKey(cr => cr.CreatedBy)
                .HasConstraintName("FK_ContentRevisions_CreatedBy")
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(cr => cr.ContentPageId)
                .HasDatabaseName("IX_ContentRevisions_ContentPageId");

            builder.HasIndex(cr => cr.CreatedAt)
                .HasDatabaseName("IX_ContentRevisions_CreatedAt")
                .IsDescending();

            builder.HasIndex(cr => new { cr.ContentPageId, cr.CreatedAt })
                .HasDatabaseName("IX_ContentRevisions_ContentPageId_CreatedAt")
                .IsDescending(false, true); // ContentPageId ASC, CreatedAt DESC

            builder.HasIndex(cr => cr.CreatedBy)
                .HasDatabaseName("IX_ContentRevisions_CreatedBy");

            // Check constraints
            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CHK_ContentRevisions_Content_NotEmpty",
                    "LENGTH(TRIM(\"Content\")) > 0"
                );

                t.HasCheckConstraint(
                    "CHK_ContentRevisions_Title_Length",
                    "LENGTH(TRIM(\"Title\")) >= 3"
                );
            });
        }
    }
}
