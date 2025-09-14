using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    public class IncidentReportConfiguration : IEntityTypeConfiguration<IncidentReport>
    {
        public void Configure(EntityTypeBuilder<IncidentReport> builder)
        {
            builder.ToTable("IncidentReports");

            builder.HasKey(i => i.Id);

            builder.Property(i => i.Id)
                .ValueGeneratedNever();

            builder.Property(i => i.ReporterId);

            builder.Property(i => i.EventId);

            builder.Property(i => i.Description)
                .IsRequired()
                .HasMaxLength(4000); // Encrypted data needs more space

            builder.Property(i => i.Severity)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(i => i.IsAnonymous)
                .IsRequired();

            builder.Property(i => i.Status)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(i => i.ReportedAt)
                .IsRequired();

            builder.Property(i => i.UpdatedAt)
                .IsRequired();

            builder.Property(i => i.ResolvedAt);

            builder.Property(i => i.ResolutionNotes)
                .HasMaxLength(2000);
                
            builder.Property(i => i.ReferenceNumber)
                .IsRequired()
                .HasMaxLength(50);
                
            builder.Property(i => i.IncidentType)
                .IsRequired()
                .HasConversion<string>();
                
            builder.Property(i => i.Location)
                .IsRequired()
                .HasMaxLength(500);
                
            builder.Property(i => i.IncidentDate)
                .IsRequired();
                
            builder.Property(i => i.RequestFollowUp)
                .IsRequired();
                
            builder.Property(i => i.PreferredContactMethod)
                .HasMaxLength(100);
                
            builder.Property(i => i.AssignedToId);

            // Configure relationships
            builder.HasOne(i => i.Reporter)
                .WithMany()
                .HasForeignKey(i => i.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(i => i.Event)
                .WithMany()
                .HasForeignKey(i => i.EventId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasOne(i => i.AssignedTo)
                .WithMany()
                .HasForeignKey(i => i.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Reviews as owned entities
            builder.OwnsMany(i => i.Reviews, review =>
            {
                review.ToTable("IncidentReviews");
                review.WithOwner().HasForeignKey("IncidentReportId");
                review.HasKey(r => r.Id);
                
                review.Property(r => r.Id)
                    .ValueGeneratedNever();
                
                review.Property(r => r.ReviewerId)
                    .IsRequired();
                
                review.Property(r => r.Findings)
                    .IsRequired()
                    .HasMaxLength(2000);
                
                review.Property(r => r.RecommendedSeverity)
                    .IsRequired()
                    .HasConversion<string>();
                
                review.Property(r => r.ReviewedAt)
                    .IsRequired();
                
                review.HasOne(r => r.Reviewer)
                    .WithMany()
                    .HasForeignKey(r => r.ReviewerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Actions as owned entities
            builder.OwnsMany(i => i.Actions, action =>
            {
                action.ToTable("IncidentActions");
                action.WithOwner().HasForeignKey("IncidentReportId");
                action.HasKey(a => a.Id);
                
                action.Property(a => a.Id)
                    .ValueGeneratedNever();
                
                action.Property(a => a.ActionType)
                    .IsRequired()
                    .HasMaxLength(100);
                
                action.Property(a => a.Description)
                    .IsRequired()
                    .HasMaxLength(1000);
                
                action.Property(a => a.PerformedById)
                    .IsRequired();
                
                action.Property(a => a.PerformedAt)
                    .IsRequired();
                
                action.HasOne(a => a.PerformedBy)
                    .WithMany()
                    .HasForeignKey(a => a.PerformedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Indexes
            builder.HasIndex(i => i.Status);
            builder.HasIndex(i => i.Severity);
            builder.HasIndex(i => i.ReportedAt);
            builder.HasIndex(i => i.IsAnonymous);
            builder.HasIndex(i => i.ReferenceNumber).IsUnique();
            builder.HasIndex(i => i.IncidentType);
            builder.HasIndex(i => i.IncidentDate);
        }
    }
}