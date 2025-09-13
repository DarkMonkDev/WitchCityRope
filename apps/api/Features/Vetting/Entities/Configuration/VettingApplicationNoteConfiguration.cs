using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.Vetting.Entities.Configuration;

/// <summary>
/// Entity Framework configuration for VettingApplicationNote
/// </summary>
public class VettingApplicationNoteConfiguration : IEntityTypeConfiguration<VettingApplicationNote>
{
    public void Configure(EntityTypeBuilder<VettingApplicationNote> builder)
    {
        // Table mapping
        builder.ToTable("VettingApplicationNotes", "public");
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.Content)
               .IsRequired()
               .HasColumnType("text");

        builder.Property(e => e.Type)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(e => e.TagsJson)
               .IsRequired()
               .HasColumnType("jsonb")
               .HasDefaultValue("[]");

        // DateTime properties
        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.UpdatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        // Indexes
        builder.HasIndex(e => e.ApplicationId)
               .HasDatabaseName("IX_VettingApplicationNotes_ApplicationId");

        builder.HasIndex(e => e.ReviewerId)
               .HasDatabaseName("IX_VettingApplicationNotes_ReviewerId");

        builder.HasIndex(e => new { e.ApplicationId, e.Type, e.CreatedAt })
               .HasDatabaseName("IX_VettingApplicationNotes_ApplicationId_Type_CreatedAt");

        builder.HasIndex(e => e.IsPrivate)
               .HasDatabaseName("IX_VettingApplicationNotes_IsPrivate");

        // GIN index for tags search
        builder.HasIndex(e => e.TagsJson)
               .HasDatabaseName("IX_VettingApplicationNotes_Tags")
               .HasMethod("gin");

        // Relationships
        builder.HasOne(e => e.Application)
               .WithMany(a => a.Notes)
               .HasForeignKey(e => e.ApplicationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Reviewer)
               .WithMany(r => r.Notes)
               .HasForeignKey(e => e.ReviewerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Attachments)
               .WithOne(a => a.Note)
               .HasForeignKey(a => a.NoteId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}