using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.EmailTemplates.Entities.Configuration;

public class GlobalEmailTemplateConfiguration : IEntityTypeConfiguration<GlobalEmailTemplate>
{
    public void Configure(EntityTypeBuilder<GlobalEmailTemplate> builder)
    {
        builder.ToTable("GlobalEmailTemplates");

        // Primary Key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        // Category (stored as integer)
        builder.Property(e => e.Category)
            .IsRequired()
            .HasConversion<int>();

        // Template Type
        builder.Property(e => e.TemplateType)
            .IsRequired()
            .HasMaxLength(50);

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

        // Variables (JSONB for PostgreSQL optimization)
        builder.Property(e => e.Variables)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        // IsActive
        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Version
        builder.Property(e => e.Version)
            .IsRequired()
            .HasDefaultValue(1);

        // Timestamps (UTC timestamptz for PostgreSQL)
        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()");

        // Foreign Key to ApplicationUser
        builder.HasOne(e => e.UpdatedByUser)
            .WithMany()
            .HasForeignKey(e => e.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique Constraint (Category, TemplateType)
        builder.HasIndex(e => new { e.Category, e.TemplateType })
            .IsUnique()
            .HasDatabaseName("UQ_GlobalEmailTemplates_Category_Type");

        // Indexes
        builder.HasIndex(e => e.Category)
            .HasDatabaseName("IX_GlobalEmailTemplates_Category");

        builder.HasIndex(e => e.UpdatedBy)
            .HasDatabaseName("IX_GlobalEmailTemplates_UpdatedBy");

        builder.HasIndex(e => e.UpdatedAt)
            .IsDescending()
            .HasDatabaseName("IX_GlobalEmailTemplates_UpdatedAt");

        // GIN Index for JSONB Variables (PostgreSQL-specific)
        builder.HasIndex(e => e.Variables)
            .HasDatabaseName("IX_GlobalEmailTemplates_Variables_Gin")
            .HasMethod("gin");

        // Check Constraints
        builder.HasCheckConstraint(
            "CHK_GlobalEmailTemplates_Category",
            "\"Category\" IN (0, 1, 2, 3, 4)"
        );

        builder.HasCheckConstraint(
            "CHK_GlobalEmailTemplates_Subject_NotEmpty",
            "LENGTH(TRIM(\"Subject\")) > 0"
        );

        builder.HasCheckConstraint(
            "CHK_GlobalEmailTemplates_HtmlBody_NotEmpty",
            "LENGTH(TRIM(\"HtmlBody\")) > 0"
        );

        builder.HasCheckConstraint(
            "CHK_GlobalEmailTemplates_PlainTextBody_NotEmpty",
            "LENGTH(TRIM(\"PlainTextBody\")) > 0"
        );

        builder.HasCheckConstraint(
            "CHK_GlobalEmailTemplates_Version",
            "\"Version\" >= 1"
        );
    }
}
