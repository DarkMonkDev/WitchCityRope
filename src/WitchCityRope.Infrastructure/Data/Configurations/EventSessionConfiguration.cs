using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    public class EventSessionConfiguration : IEntityTypeConfiguration<EventSession>
    {
        public void Configure(EntityTypeBuilder<EventSession> builder)
        {
            builder.ToTable("EventSessions", "public");
            
            // Primary Key
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .ValueGeneratedNever(); // Set by domain logic
            
            // Properties
            builder.Property(e => e.SessionIdentifier)
                .IsRequired()
                .HasMaxLength(10);
            
            builder.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            
            builder.Property(e => e.StartDateTime)
                .IsRequired()
                .HasColumnType("timestamp with time zone");
            
            builder.Property(e => e.EndDateTime)
                .IsRequired()
                .HasColumnType("timestamp with time zone");
            
            builder.Property(e => e.Capacity)
                .IsRequired();
            
            builder.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            
            builder.Property(e => e.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone");
            
            builder.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone");
            
            // Foreign Key to Event
            builder.Property(e => e.EventId)
                .IsRequired();
            
            // Navigation will be configured by Event entity
            
            // Indexes
            builder.HasIndex(e => new { e.EventId, e.IsActive })
                .HasDatabaseName("IX_EventSessions_EventId_IsActive");
            
            // Unique constraint for session identifier per event
            builder.HasIndex(e => new { e.EventId, e.SessionIdentifier })
                .IsUnique()
                .HasDatabaseName("UQ_EventSessions_EventId_SessionIdentifier");
        }
    }
}