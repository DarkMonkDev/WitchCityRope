using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Api.Features.Participation.Entities;

namespace WitchCityRope.Api.Features.Participation.Configuration;

/// <summary>
/// Entity Framework configuration for AuthorizedContact entity.
/// Implements soft-delete pattern via RevokedAt and partial unique index.
/// </summary>
public class AuthorizedContactConfiguration : IEntityTypeConfiguration<AuthorizedContact>
{
    public void Configure(EntityTypeBuilder<AuthorizedContact> builder)
    {
        // Table mapping
        builder.ToTable("AuthorizedContacts", "public");
        builder.HasKey(ac => ac.Id);

        // Property configurations
        builder.Property(ac => ac.Id)
               .ValueGeneratedOnAdd();

        builder.Property(ac => ac.PrincipalId)
               .IsRequired();

        builder.Property(ac => ac.DelegateId)
               .IsRequired();

        // UTC DateTime handling for PostgreSQL
        builder.Property(ac => ac.CreatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(ac => ac.UpdatedAt)
               .IsRequired()
               .HasColumnType("timestamptz");

        builder.Property(ac => ac.RevokedAt)
               .HasColumnType("timestamptz");

        builder.Property(ac => ac.RevokedReason)
               .HasMaxLength(500);

        // Ignore computed property
        builder.Ignore(ac => ac.IsActive);

        // Foreign key relationships
        builder.HasOne(ac => ac.Principal)
               .WithMany()
               .HasForeignKey(ac => ac.PrincipalId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ac => ac.Delegate)
               .WithMany()
               .HasForeignKey(ac => ac.DelegateId)
               .OnDelete(DeleteBehavior.Cascade);

        // Partial unique constraint: one ACTIVE authorization per Principal+Delegate pair
        // Allows re-authorization after revocation (revoked records are not constrained)
        // BR-003: Self-authorization blocked by CHECK constraint below
        // BR-004: Mutual authorization allowed (A->B and B->A are separate records)
        builder.HasIndex(ac => new { ac.PrincipalId, ac.DelegateId })
               .IsUnique()
               .HasDatabaseName("UQ_AuthorizedContacts_Principal_Delegate_Active")
               .HasFilter("\"RevokedAt\" IS NULL");

        // Indexes for querying
        // "Who can act on my behalf?" (Principal's view)
        builder.HasIndex(ac => ac.PrincipalId)
               .HasDatabaseName("IX_AuthorizedContacts_PrincipalId");

        // "Who have I been authorized to act for?" (Delegate's view)
        builder.HasIndex(ac => ac.DelegateId)
               .HasDatabaseName("IX_AuthorizedContacts_DelegateId");

        // Composite index for common query: active authorizations for a delegate
        // Used when delegate is buying tickets/RSVPing - need to find their principals
        builder.HasIndex(ac => new { ac.DelegateId, ac.RevokedAt })
               .HasDatabaseName("IX_AuthorizedContacts_DelegateId_RevokedAt");

        // Business rule constraints
        // BR-003: Self-authorization blocked
        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_AuthorizedContacts_NoSelfAuthorization",
            "\"PrincipalId\" != \"DelegateId\""));
    }
}
