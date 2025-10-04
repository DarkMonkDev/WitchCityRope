using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.Vetting.Entities.Configuration;

/// <summary>
/// Entity Framework configuration for VettingEmailTemplate
/// Follows WitchCityRope patterns with PostgreSQL optimizations
/// </summary>
public class VettingEmailTemplateConfiguration : IEntityTypeConfiguration<VettingEmailTemplate>
{
    public void Configure(EntityTypeBuilder<VettingEmailTemplate> builder)
    {
        // Table mapping
        builder.ToTable("VettingEmailTemplates", "public");
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.TemplateType)
               .IsRequired()
               .HasConversion<int>();

        builder.Property(e => e.Subject)
               .IsRequired()
               .HasMaxLength(200);

        // Email bodies - HTML and Plain Text
        builder.Property(e => e.HtmlBody)
               .IsRequired()
               .HasColumnType("text");

        builder.Property(e => e.PlainTextBody)
               .IsRequired()
               .HasColumnType("text");

        // Variables for template substitution (stored as JSON)
        builder.Property(e => e.Variables)
               .IsRequired()
               .HasColumnType("jsonb")
               .HasDefaultValue("{}");

        builder.Property(e => e.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.Property(e => e.Version)
               .IsRequired()
               .HasDefaultValue(1);

        // DateTime properties - CRITICAL: Use timestamptz for PostgreSQL
        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.UpdatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.LastModified)
               .IsRequired()
               .HasColumnType("timestamptz");

        // Indexes
        builder.HasIndex(e => e.TemplateType)
               .IsUnique()
               .HasDatabaseName("UQ_VettingEmailTemplates_TemplateType");

        builder.HasIndex(e => e.IsActive)
               .HasDatabaseName("IX_VettingEmailTemplates_IsActive")
               .HasFilter("\"IsActive\" = TRUE");

        builder.HasIndex(e => e.UpdatedAt)
               .HasDatabaseName("IX_VettingEmailTemplates_UpdatedAt");

        // GIN index for JSONB variables column
        builder.HasIndex(e => e.Variables)
               .HasDatabaseName("IX_VettingEmailTemplates_Variables")
               .HasMethod("gin");

        // Relationships
        builder.HasOne(e => e.UpdatedByUser)
               .WithMany()
               .HasForeignKey(e => e.UpdatedBy)
               .OnDelete(DeleteBehavior.Restrict);

        // Check constraints
        builder.ToTable("VettingEmailTemplates", t => t.HasCheckConstraint(
            "CHK_VettingEmailTemplates_Subject_Length",
            "LENGTH(\"Subject\") BETWEEN 5 AND 200"
        ));

        builder.ToTable("VettingEmailTemplates", t => t.HasCheckConstraint(
            "CHK_VettingEmailTemplates_HtmlBody_Length",
            "LENGTH(\"HtmlBody\") >= 10"
        ));

        builder.ToTable("VettingEmailTemplates", t => t.HasCheckConstraint(
            "CHK_VettingEmailTemplates_PlainTextBody_Length",
            "LENGTH(\"PlainTextBody\") >= 10"
        ));

        // Ignore obsolete Body property - it's just a getter/setter for HtmlBody
        builder.Ignore(e => e.Body);
    }
}
