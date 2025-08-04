using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    public class VolunteerAssignmentConfiguration : IEntityTypeConfiguration<VolunteerAssignment>
    {
        public void Configure(EntityTypeBuilder<VolunteerAssignment> builder)
        {
            builder.ToTable("VolunteerAssignments");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(a => a.HasTicket)
                .IsRequired();

            builder.Property(a => a.TicketPrice)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(a => a.BackgroundCheckVerified)
                .IsRequired();

            builder.Property(a => a.AssignedAt)
                .IsRequired();

            builder.Property(a => a.UpdatedAt)
                .IsRequired();

            // Relationship with VolunteerTask
            builder.HasOne(a => a.Task)
                .WithMany(t => t.Assignments)
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // User relationship is handled via UserId foreign key only (no navigation property)
            // The WitchCityRopeUser entity is used instead of Core.User

            // Indexes
            builder.HasIndex(a => a.TaskId);
            builder.HasIndex(a => a.UserId);
            builder.HasIndex(a => new { a.TaskId, a.UserId });
            builder.HasIndex(a => a.Status);
        }
    }
}