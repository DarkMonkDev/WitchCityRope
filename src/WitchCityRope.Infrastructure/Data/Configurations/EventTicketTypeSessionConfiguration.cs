using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    public class EventTicketTypeSessionConfiguration : IEntityTypeConfiguration<EventTicketTypeSession>
    {
        public void Configure(EntityTypeBuilder<EventTicketTypeSession> builder)
        {
            builder.ToTable("EventTicketTypeSessions", "public");
            
            // Primary Key
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .ValueGeneratedNever(); // Set by domain logic
            
            // Foreign Key to EventTicketType
            builder.Property(e => e.EventTicketTypeId)
                .IsRequired();
            
            builder.HasOne(e => e.EventTicketType)
                .WithMany(tt => tt.SessionInclusions)
                .HasForeignKey(e => e.EventTicketTypeId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Foreign Key to EventSession
            builder.Property(e => e.EventSessionId)
                .IsRequired();
            
            builder.HasOne(e => e.EventSession)
                .WithMany(s => s.TicketTypeInclusions)
                .HasForeignKey(e => e.EventSessionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Timestamps
            builder.Property(e => e.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone");
            
            builder.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone");
            
            // Indexes
            builder.HasIndex(e => e.EventTicketTypeId)
                .HasDatabaseName("IX_EventTicketTypeSessions_EventTicketTypeId");
            
            builder.HasIndex(e => e.EventSessionId)
                .HasDatabaseName("IX_EventTicketTypeSessions_EventSessionId");
            
            // Unique constraint for ticket type and session combination
            builder.HasIndex(e => new { e.EventTicketTypeId, e.EventSessionId })
                .IsUnique()
                .HasDatabaseName("UQ_EventTicketTypeSessions_TicketType_Session");
        }
    }
}