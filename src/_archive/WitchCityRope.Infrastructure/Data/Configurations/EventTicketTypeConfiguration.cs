using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    public class EventTicketTypeConfiguration : IEntityTypeConfiguration<EventTicketType>
    {
        public void Configure(EntityTypeBuilder<EventTicketType> builder)
        {
            builder.ToTable("EventTicketTypes");

            builder.HasKey(ett => ett.Id);

            builder.Property(ett => ett.Id)
                .ValueGeneratedNever();

            builder.Property(ett => ett.EventId)
                .IsRequired();

            builder.Property(ett => ett.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(ett => ett.Description)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(ett => ett.MinPrice)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(ett => ett.MaxPrice)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(ett => ett.QuantityAvailable)
                .IsRequired(false); // Nullable for unlimited

            builder.Property(ett => ett.SalesEndDate)
                .IsRequired(false);

            builder.Property(ett => ett.IsRsvpMode)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(ett => ett.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(ett => ett.CreatedAt)
                .IsRequired();

            builder.Property(ett => ett.UpdatedAt)
                .IsRequired();

            // Configure relationships
            builder.HasOne(ett => ett.Event)
                .WithMany(e => e.TicketTypes)
                .HasForeignKey(ett => ett.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(ett => ett.TicketTypeSessions)
                .WithOne(tts => tts.TicketType)
                .HasForeignKey(tts => tts.TicketTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(ett => ett.EventId);
            builder.HasIndex(ett => new { ett.EventId, ett.Name })
                .IsUnique()
                .HasDatabaseName("IX_EventTicketTypes_EventId_Name");
            builder.HasIndex(ett => new { ett.EventId, ett.IsActive });
            builder.HasIndex(ett => ett.SalesEndDate);

            // Constraints
            builder.ToTable(t =>
            {
                t.HasCheckConstraint("CK_EventTicketTypes_MinPrice", "\"MinPrice\" >= 0");
                t.HasCheckConstraint("CK_EventTicketTypes_MaxPrice", "\"MaxPrice\" >= 0");
                t.HasCheckConstraint("CK_EventTicketTypes_Price_Range", "\"MinPrice\" <= \"MaxPrice\"");
                t.HasCheckConstraint("CK_EventTicketTypes_QuantityAvailable", "\"QuantityAvailable\" IS NULL OR \"QuantityAvailable\" > 0");
            });
        }
    }
}