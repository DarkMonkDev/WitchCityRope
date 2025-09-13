using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.Vetting.Entities.Configuration;

/// <summary>
/// Entity Framework configuration for VettingNoteAttachment
/// </summary>
public class VettingNoteAttachmentConfiguration : IEntityTypeConfiguration<VettingNoteAttachment>
{
    public void Configure(EntityTypeBuilder<VettingNoteAttachment> builder)
    {
        // Table mapping
        builder.ToTable("VettingNoteAttachments", "public");
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.FileName)
               .IsRequired()
               .HasMaxLength(255);

        builder.Property(e => e.FileExtension)
               .IsRequired()
               .HasMaxLength(10);

        builder.Property(e => e.MimeType)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(e => e.StoragePath)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(e => e.FileHash)
               .IsRequired()
               .HasMaxLength(64); // SHA-256 hash length

        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        // Indexes
        builder.HasIndex(e => e.NoteId)
               .HasDatabaseName("IX_VettingNoteAttachments_NoteId");

        builder.HasIndex(e => e.FileHash)
               .HasDatabaseName("IX_VettingNoteAttachments_FileHash");

        builder.HasIndex(e => new { e.NoteId, e.FileName })
               .HasDatabaseName("IX_VettingNoteAttachments_NoteId_FileName");

        builder.HasIndex(e => e.IsConfidential)
               .HasDatabaseName("IX_VettingNoteAttachments_IsConfidential");

        // Relationships
        builder.HasOne(e => e.Note)
               .WithMany(n => n.Attachments)
               .HasForeignKey(e => e.NoteId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}