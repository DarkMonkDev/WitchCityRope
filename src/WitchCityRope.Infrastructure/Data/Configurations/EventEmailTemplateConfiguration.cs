using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    public class EventEmailTemplateConfiguration : IEntityTypeConfiguration<EventEmailTemplate>
    {
        public void Configure(EntityTypeBuilder<EventEmailTemplate> builder)
        {
            builder.ToTable("EventEmailTemplates");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Type)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(t => t.Subject)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(t => t.Body)
                .IsRequired();

            builder.Property(t => t.IsActive)
                .IsRequired();

            builder.Property(t => t.CreatedAt)
                .IsRequired();

            builder.Property(t => t.UpdatedAt)
                .IsRequired();

            // Relationship with Event
            builder.HasOne(t => t.Event)
                .WithMany(e => e.EmailTemplates)
                .HasForeignKey(t => t.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(t => t.EventId);
            builder.HasIndex(t => new { t.EventId, t.Type });
            builder.HasIndex(t => t.IsActive);
        }
    }
}