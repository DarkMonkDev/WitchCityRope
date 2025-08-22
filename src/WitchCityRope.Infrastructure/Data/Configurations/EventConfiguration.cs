using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.ValueObjects;
using System.Text.Json;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.ToTable("Events");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .ValueGeneratedNever();

            builder.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(4000);

            builder.Property(e => e.StartDate)
                .IsRequired();

            builder.Property(e => e.EndDate)
                .IsRequired();

            builder.Property(e => e.Capacity)
                .IsRequired();

            builder.Property(e => e.EventType)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(e => e.Location)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(e => e.IsPublished)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.CreatedAt)
                .IsRequired();

            builder.Property(e => e.UpdatedAt)
                .IsRequired();

            // Configure PricingTiers as JSON
            builder.Property(e => e.PricingTiers)
                .HasConversion(
                    v => JsonSerializer.Serialize(v.Select(m => new { Amount = m.Amount, Currency = m.Currency }), (JsonSerializerOptions?)null),
                    v => v == null 
                        ? new List<Money>()
                        : JsonSerializer.Deserialize<List<JsonElement>>(v, (JsonSerializerOptions?)null) == null
                            ? new List<Money>()
                            : JsonSerializer.Deserialize<List<JsonElement>>(v, (JsonSerializerOptions?)null)!
                                .Select(m => Money.Create(
                                    m.GetProperty("Amount").GetDecimal(), 
                                    m.GetProperty("Currency").GetString() ?? "USD"))
                                .ToList()
                )
                .HasColumnType("TEXT");

            // Configure many-to-many relationship with organizers
            builder.HasMany(e => e.Organizers)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "EventOrganizers",
                    j => j.HasOne<User>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<Event>().WithMany().HasForeignKey("EventId").OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("EventId", "UserId");
                        j.ToTable("EventOrganizers");
                    });

            // Configure relationships
            builder.HasMany(e => e.Registrations)
                .WithOne(r => r.Event)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(e => e.StartDate);
            builder.HasIndex(e => e.EventType);
            builder.HasIndex(e => e.IsPublished);
            builder.HasIndex(e => new { e.IsPublished, e.StartDate });
        }
    }
}