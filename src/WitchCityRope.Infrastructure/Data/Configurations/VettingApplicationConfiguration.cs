using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;
using System.Text.Json;

namespace WitchCityRope.Infrastructure.Data.Configurations
{
    public class VettingApplicationConfiguration : IEntityTypeConfiguration<VettingApplication>
    {
        public void Configure(EntityTypeBuilder<VettingApplication> builder)
        {
            builder.ToTable("VettingApplications");

            builder.HasKey(v => v.Id);

            builder.Property(v => v.Id)
                .ValueGeneratedNever();

            builder.Property(v => v.ApplicantId)
                .IsRequired();

            builder.Property(v => v.ExperienceLevel)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(v => v.Interests)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(v => v.SafetyKnowledge)
                .IsRequired()
                .HasMaxLength(2000);
                
            builder.Property(v => v.ExperienceDescription)
                .HasMaxLength(2000);
                
            builder.Property(v => v.ConsentUnderstanding)
                .HasMaxLength(2000);
                
            builder.Property(v => v.WhyJoin)
                .HasMaxLength(2000);

            builder.Property(v => v.Status)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(v => v.SubmittedAt)
                .IsRequired();

            builder.Property(v => v.UpdatedAt)
                .IsRequired();

            builder.Property(v => v.ReviewedAt);

            builder.Property(v => v.DecisionNotes)
                .HasMaxLength(1000);

            // Configure References as JSON
            builder.Property(v => v.References)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null)
                )
                .HasColumnType("TEXT");

            // Configure relationship with User
            builder.HasOne(v => v.Applicant)
                .WithMany(u => u.VettingApplications)
                .HasForeignKey(v => v.ApplicantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Reviews as owned entities
            builder.OwnsMany(v => v.Reviews, review =>
            {
                review.ToTable("VettingReviews");
                review.WithOwner().HasForeignKey("VettingApplicationId");
                review.HasKey(r => r.Id);
                
                review.Property(r => r.Id)
                    .ValueGeneratedNever();
                
                review.Property(r => r.ReviewerId)
                    .IsRequired();
                
                review.Property(r => r.Recommendation)
                    .IsRequired();
                
                review.Property(r => r.Notes)
                    .IsRequired()
                    .HasMaxLength(1000);
                
                review.Property(r => r.ReviewedAt)
                    .IsRequired();
                
                review.HasOne(r => r.Reviewer)
                    .WithMany()
                    .HasForeignKey(r => r.ReviewerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Indexes
            builder.HasIndex(v => v.ApplicantId);
            builder.HasIndex(v => v.Status);
            builder.HasIndex(v => v.SubmittedAt);
        }
    }
}