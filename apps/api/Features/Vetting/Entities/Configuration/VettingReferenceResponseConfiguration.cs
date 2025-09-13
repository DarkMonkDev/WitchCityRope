using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WitchCityRope.Api.Features.Vetting.Entities.Configuration;

/// <summary>
/// Entity Framework configuration for VettingReferenceResponse
/// </summary>
public class VettingReferenceResponseConfiguration : IEntityTypeConfiguration<VettingReferenceResponse>
{
    public void Configure(EntityTypeBuilder<VettingReferenceResponse> builder)
    {
        // Table mapping
        builder.ToTable("VettingReferenceResponses", "public");
        builder.HasKey(e => e.Id);

        // Encrypted properties
        builder.Property(e => e.EncryptedRelationshipDuration)
               .IsRequired()
               .HasColumnType("text");

        builder.Property(e => e.EncryptedExperienceAssessment)
               .IsRequired()
               .HasColumnType("text");

        builder.Property(e => e.EncryptedSafetyConcerns)
               .HasColumnType("text");

        builder.Property(e => e.EncryptedCommunityReadiness)
               .IsRequired()
               .HasColumnType("text");

        builder.Property(e => e.EncryptedAdditionalComments)
               .HasColumnType("text");

        // Enum configuration
        builder.Property(e => e.Recommendation)
               .IsRequired()
               .HasConversion<int>();

        // Metadata properties
        builder.Property(e => e.ResponseIpAddress)
               .IsRequired()
               .HasMaxLength(45); // IPv6 support

        builder.Property(e => e.ResponseUserAgent)
               .HasMaxLength(500);

        // DateTime properties
        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(e => e.UpdatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        // Indexes
        builder.HasIndex(e => e.ReferenceId)
               .IsUnique()
               .HasDatabaseName("IX_VettingReferenceResponses_ReferenceId");

        builder.HasIndex(e => e.Recommendation)
               .HasDatabaseName("IX_VettingReferenceResponses_Recommendation");

        builder.HasIndex(e => e.IsCompleted)
               .HasDatabaseName("IX_VettingReferenceResponses_IsCompleted");

        // Relationships
        builder.HasOne(e => e.Reference)
               .WithOne(r => r.Response)
               .HasForeignKey<VettingReferenceResponse>(e => e.ReferenceId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}