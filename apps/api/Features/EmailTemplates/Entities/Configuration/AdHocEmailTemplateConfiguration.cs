using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.EmailTemplates.Entities.Configuration;

public class AdHocEmailTemplateConfiguration : IEntityTypeConfiguration<AdHocEmailTemplate>
{
    public void Configure(EntityTypeBuilder<AdHocEmailTemplate> builder)
    {
        builder.ToTable("AdHocEmailTemplates");

        // Primary Key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        // Template Name
        builder.Property(e => e.TemplateName)
            .IsRequired()
            .HasMaxLength(200);

        // Subject
        builder.Property(e => e.Subject)
            .IsRequired()
            .HasMaxLength(200);

        // HTML Body
        builder.Property(e => e.HtmlBody)
            .IsRequired()
            .HasColumnType("text");

        // Plain Text Body
        builder.Property(e => e.PlainTextBody)
            .IsRequired()
            .HasColumnType("text");

        // Timestamp (UTC timestamptz)
        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()");

        // Foreign Key to ApplicationUser
        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => e.CreatedBy)
            .HasDatabaseName("IX_AdHocEmailTemplates_CreatedBy");

        builder.HasIndex(e => e.CreatedAt)
            .IsDescending()
            .HasDatabaseName("IX_AdHocEmailTemplates_CreatedAt");

        builder.HasIndex(e => e.TemplateName)
            .HasDatabaseName("IX_AdHocEmailTemplates_TemplateName");

        // Check Constraints
        builder.HasCheckConstraint(
            "CHK_AdHocEmailTemplates_TemplateName_NotEmpty",
            "LENGTH(TRIM(\"TemplateName\")) > 0"
        );

        builder.HasCheckConstraint(
            "CHK_AdHocEmailTemplates_Subject_NotEmpty",
            "LENGTH(TRIM(\"Subject\")) > 0"
        );

        builder.HasCheckConstraint(
            "CHK_AdHocEmailTemplates_HtmlBody_NotEmpty",
            "LENGTH(TRIM(\"HtmlBody\")) > 0"
        );

        builder.HasCheckConstraint(
            "CHK_AdHocEmailTemplates_PlainTextBody_NotEmpty",
            "LENGTH(TRIM(\"PlainTextBody\")) > 0"
        );
    }
}
