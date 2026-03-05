using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.EmailTemplates.Entities.Configuration;

public class EventEmailTemplateConfiguration : IEntityTypeConfiguration<EventEmailTemplate>
{
    public void Configure(EntityTypeBuilder<EventEmailTemplate> builder)
    {
        builder.ToTable("EventEmailTemplates", "public", t =>
        {
            t.HasCheckConstraint(
                "CHK_EventEmailTemplates_Subject_NotEmpty",
                "LENGTH(TRIM(\"Subject\")) > 0"
            );
            t.HasCheckConstraint(
                "CHK_EventEmailTemplates_OverrideTimingOffsetDays",
                "\"OverrideTimingOffsetDays\" IS NULL OR (\"OverrideTimingOffsetDays\" >= -365 AND \"OverrideTimingOffsetDays\" <= 365)"
            );
            t.HasCheckConstraint(
                "CHK_EventEmailTemplates_OverrideTimingOffsetHours",
                "\"OverrideTimingOffsetHours\" IS NULL OR (\"OverrideTimingOffsetHours\" >= -23 AND \"OverrideTimingOffsetHours\" <= 23)"
            );
            t.HasCheckConstraint(
                "CHK_EventEmailTemplates_OverrideRecipientGroup",
                "\"OverrideRecipientGroup\" IS NULL OR \"OverrideRecipientGroup\" IN (0, 1, 2, 3)"
            );
            t.HasCheckConstraint(
                "CHK_EventEmailTemplates_HtmlBody_NotEmpty",
                "LENGTH(TRIM(\"HtmlBody\")) > 0"
            );
            t.HasCheckConstraint(
                "CHK_EventEmailTemplates_PlainTextBody_NotEmpty",
                "LENGTH(TRIM(\"PlainTextBody\")) > 0"
            );
        });

        // Primary Key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        // EventId (required, cascade delete)
        builder.Property(e => e.EventId)
            .IsRequired();

        builder.HasOne(e => e.Event)
            .WithMany()
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // GlobalTemplateId (reference only, NO foreign key constraint)
        builder.Property(e => e.GlobalTemplateId)
            .IsRequired();

        // NOTE: GlobalTemplate navigation is intentionally NOT configured as a foreign key
        // GlobalTemplateId is a reference-only field for UI purposes
        // No foreign key constraint - global template can be deleted/changed independently

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

        // TargetSessions (PostgreSQL array)
        builder.Property(e => e.TargetSessions)
            .HasColumnType("text[]")
            .HasDefaultValue(Array.Empty<string>());

        // RecipientGroup
        builder.Property(e => e.RecipientGroup)
            .HasMaxLength(100);

        // IsCustomized
        builder.Property(e => e.IsCustomized)
            .IsRequired()
            .HasDefaultValue(true);

        // Timestamps (UTC timestamptz)
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

        // Unique Constraint (EventId, TemplateType)
        builder.HasIndex(e => new { e.EventId, e.TemplateType })
            .IsUnique()
            .HasDatabaseName("UQ_EventEmailTemplates_EventId_Type");

        // Indexes
        builder.HasIndex(e => e.EventId)
            .HasDatabaseName("IX_EventEmailTemplates_EventId");

        builder.HasIndex(e => e.UpdatedBy)
            .HasDatabaseName("IX_EventEmailTemplates_UpdatedBy");

        builder.HasIndex(e => e.UpdatedAt)
            .IsDescending()
            .HasDatabaseName("IX_EventEmailTemplates_UpdatedAt");

        // Override fields
        builder.Property(e => e.OverrideTriggerEnabled)
            .IsRequired(false); // Nullable

        builder.Property(e => e.OverrideTimingOffsetDays)
            .IsRequired(false); // Nullable

        builder.Property(e => e.OverrideTimingOffsetHours)
            .IsRequired(false); // Nullable

        builder.Property(e => e.OverrideRecipientGroup)
            .IsRequired(false) // Nullable
            .HasConversion<int>(); // Stored as int when set

    }
}
