using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    public class EventSessionConfiguration : IEntityTypeConfiguration<EventSession>
    {
        public void Configure(EntityTypeBuilder<EventSession> builder)
        {
            builder.ToTable("EventSessions");

            builder.HasKey(es => es.Id);

            builder.Property(es => es.Id)
                .ValueGeneratedNever();

            // Add alternate key for SessionIdentifier to support foreign key relationships
            builder.HasAlternateKey(es => new { es.EventId, es.SessionIdentifier });

            builder.Property(es => es.EventId)
                .IsRequired();

            builder.Property(es => es.SessionIdentifier)
                .IsRequired()
                .HasMaxLength(10); // S1, S2, S3, etc.

            builder.Property(es => es.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(es => es.Date)
                .IsRequired();

            builder.Property(es => es.StartTime)
                .IsRequired();

            builder.Property(es => es.EndTime)
                .IsRequired();

            builder.Property(es => es.Capacity)
                .IsRequired();

            builder.Property(es => es.IsRequired)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(es => es.RegisteredCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(es => es.CreatedAt)
                .IsRequired();

            builder.Property(es => es.UpdatedAt)
                .IsRequired();

            // Configure relationships
            builder.HasOne(es => es.Event)
                .WithMany(e => e.Sessions)
                .HasForeignKey(es => es.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(es => es.EventId);
            builder.HasIndex(es => new { es.EventId, es.SessionIdentifier })
                .IsUnique()
                .HasDatabaseName("IX_EventSessions_EventId_SessionIdentifier");
            builder.HasIndex(es => new { es.EventId, es.Date });
            builder.HasIndex(es => new { es.Date, es.StartTime });

            // Constraints
            builder.ToTable(t =>
            {
                t.HasCheckConstraint("CK_EventSessions_Capacity", "\"Capacity\" > 0");
                t.HasCheckConstraint("CK_EventSessions_RegisteredCount", "\"RegisteredCount\" >= 0");
                t.HasCheckConstraint("CK_EventSessions_RegisteredCount_LTE_Capacity", "\"RegisteredCount\" <= \"Capacity\"");
            });
        }
    }
}