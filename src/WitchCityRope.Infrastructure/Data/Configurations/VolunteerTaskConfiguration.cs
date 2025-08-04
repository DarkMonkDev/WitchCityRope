using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    public class VolunteerTaskConfiguration : IEntityTypeConfiguration<VolunteerTask>
    {
        public void Configure(EntityTypeBuilder<VolunteerTask> builder)
        {
            builder.ToTable("VolunteerTasks");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(t => t.StartTime)
                .IsRequired();

            builder.Property(t => t.EndTime)
                .IsRequired();

            builder.Property(t => t.RequiredVolunteers)
                .IsRequired();

            // Relationship with Event
            builder.HasOne(t => t.Event)
                .WithMany(e => e.VolunteerTasks)
                .HasForeignKey(t => t.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship with VolunteerAssignments
            builder.HasMany(t => t.Assignments)
                .WithOne(a => a.Task)
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(t => t.EventId);
            builder.HasIndex(t => new { t.EventId, t.StartTime });
        }
    }
}