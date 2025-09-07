using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    public class EventTicketTypeConfiguration : IEntityTypeConfiguration<EventTicketType>
    {
        public void Configure(EntityTypeBuilder<EventTicketType> builder)
        {
            builder.ToTable("EventTicketTypes", "public");
            
            // Primary Key
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .ValueGeneratedNever(); // Set by domain logic
            
            // Properties
            builder.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            
            builder.Property(e => e.TicketType)
                .IsRequired()
                .HasConversion<int>(); // Store enum as int
            
            builder.Property(e => e.MinPrice)
                .IsRequired()
                .HasColumnType("decimal(10,2)");
            
            builder.Property(e => e.MaxPrice)
                .IsRequired()
                .HasColumnType("decimal(10,2)");
            
            builder.Property(e => e.QuantityAvailable)
                .IsRequired(false); // Nullable for unlimited
            
            builder.Property(e => e.SalesEndDateTime)
                .IsRequired(false)
                .HasColumnType("timestamp with time zone");
            
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
            
            // Navigation to Registrations
            builder.HasMany(e => e.Registrations)
                .WithOne(r => r.EventTicketType)
                .HasForeignKey(r => r.EventTicketTypeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
            
            // Indexes
            builder.HasIndex(e => new { e.EventId, e.IsActive })
                .HasDatabaseName("IX_EventTicketTypes_EventId_IsActive");
        }
    }
}