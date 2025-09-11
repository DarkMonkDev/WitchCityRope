using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    public class EventTicketTypeSessionConfiguration : IEntityTypeConfiguration<EventTicketTypeSession>
    {
        public void Configure(EntityTypeBuilder<EventTicketTypeSession> builder)
        {
            builder.ToTable("EventTicketTypeSessions");

            builder.HasKey(etts => etts.Id);

            builder.Property(etts => etts.Id)
                .ValueGeneratedNever();

            builder.Property(etts => etts.TicketTypeId)
                .IsRequired();

            builder.Property(etts => etts.SessionIdentifier)
                .IsRequired()
                .HasMaxLength(10); // S1, S2, S3, etc.

            builder.Property(etts => etts.CreatedAt)
                .IsRequired();

            // Configure relationships
            builder.HasOne(etts => etts.TicketType)
                .WithMany(ett => ett.TicketTypeSessions)
                .HasForeignKey(etts => etts.TicketTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure relationship with EventSession via SessionIdentifier
            // This is a manual foreign key relationship since we're linking by string identifier
            // Note: EF Core will handle the relationship through the SessionIdentifier property
            // but we need to be careful about foreign key constraints in the database

            // Indexes
            builder.HasIndex(etts => etts.TicketTypeId);
            builder.HasIndex(etts => etts.SessionIdentifier);
            builder.HasIndex(etts => new { etts.TicketTypeId, etts.SessionIdentifier })
                .IsUnique()
                .HasDatabaseName("IX_EventTicketTypeSessions_TicketTypeId_SessionIdentifier");
        }
    }
}