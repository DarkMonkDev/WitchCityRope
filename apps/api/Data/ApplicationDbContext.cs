using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Data;

/// <summary>
/// Entity Framework Core DbContext for PostgreSQL with ASP.NET Core Identity
/// Configured for vertical slice proof-of-concept with authentication
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Events table for event management
    /// </summary>
    public DbSet<Event> Events { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Call base to configure Identity tables
        base.OnModelCreating(modelBuilder);

        // Configure Identity tables to use public schema (matching existing database)
        modelBuilder.Entity<ApplicationUser>().ToTable("Users", "public");
        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("Roles", "public");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles", "public");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims", "public");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins", "public");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens", "public");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims", "public");

        // Configure ApplicationUser entity with custom properties
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.SceneName)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamptz");

            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasColumnType("timestamptz");

            entity.Property(e => e.LastLoginAt)
                .HasColumnType("timestamptz");

            // Configure additional fields to match existing schema
            entity.Property(e => e.EncryptedLegalName)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.DateOfBirth)
                .IsRequired()
                .HasColumnType("timestamptz");

            entity.Property(e => e.Role)
                .IsRequired()
                .HasColumnType("text");

            entity.Property(e => e.PronouncedName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Pronouns)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.EmailVerificationToken)
                .IsRequired()
                .HasColumnType("text");

            entity.Property(e => e.EmailVerificationTokenCreatedAt)
                .HasColumnType("timestamptz");

            entity.Property(e => e.LockedOutUntil)
                .HasColumnType("timestamptz");

            entity.Property(e => e.LastPasswordChangeAt)
                .HasColumnType("timestamptz");

            // Indexes to match existing schema
            entity.HasIndex(e => e.SceneName)
                .IsUnique()
                .HasDatabaseName("IX_Users_SceneName");

            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_Users_IsActive");

            entity.HasIndex(e => e.IsVetted)
                .HasDatabaseName("IX_Users_IsVetted");

            entity.HasIndex(e => e.Role)
                .HasDatabaseName("IX_Users_Role");
        });

        // Event entity configuration - matches existing database structure
        modelBuilder.Entity<Event>(entity =>
        {
            // Table mapping
            entity.ToTable("Events", "public");
            entity.HasKey(e => e.Id);

            // Property configurations matching existing PostgreSQL schema
            entity.Property(e => e.Title)
                  .IsRequired();

            entity.Property(e => e.Description)
                  .IsRequired();

            // CRITICAL: Use timestamptz for PostgreSQL timezone awareness
            entity.Property(e => e.StartDate)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            entity.Property(e => e.EndDate)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            entity.Property(e => e.Capacity)
                  .IsRequired();

            entity.Property(e => e.EventType)
                  .IsRequired()
                  .HasColumnType("text");

            entity.Property(e => e.Location)
                  .IsRequired();

            entity.Property(e => e.IsPublished)
                  .IsRequired();

            entity.Property(e => e.CreatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            entity.Property(e => e.UpdatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            entity.Property(e => e.PricingTiers)
                  .IsRequired()
                  .HasColumnType("text");
        });
    }

    /// <summary>
    /// Override SaveChangesAsync to ensure UTC dates
    /// This prevents PostgreSQL timezone issues
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Ensures all DateTime fields are set to UTC
    /// Critical for PostgreSQL TIMESTAMPTZ compatibility
    /// </summary>
    private void UpdateAuditFields()
    {
        // Handle Event entities
        var eventEntries = ChangeTracker.Entries<Event>();
        foreach (var entry in eventEntries)
        {
            if (entry.State == EntityState.Added)
            {
                // Ensure CreatedAt and UpdatedAt are UTC
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;

                // Ensure StartDate and EndDate are UTC if not already
                if (entry.Entity.StartDate.Kind != DateTimeKind.Utc)
                {
                    entry.Entity.StartDate = DateTime.SpecifyKind(entry.Entity.StartDate, DateTimeKind.Utc);
                }
                if (entry.Entity.EndDate.Kind != DateTimeKind.Utc)
                {
                    entry.Entity.EndDate = DateTime.SpecifyKind(entry.Entity.EndDate, DateTimeKind.Utc);
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                // Update timestamp on modification
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Handle ApplicationUser entities
        var userEntries = ChangeTracker.Entries<ApplicationUser>();
        foreach (var entry in userEntries)
        {
            if (entry.State == EntityState.Added)
            {
                // Ensure CreatedAt and UpdatedAt are UTC
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;

                // Ensure LastLoginAt is UTC if set
                if (entry.Entity.LastLoginAt.HasValue && entry.Entity.LastLoginAt.Value.Kind != DateTimeKind.Utc)
                {
                    entry.Entity.LastLoginAt = DateTime.SpecifyKind(entry.Entity.LastLoginAt.Value, DateTimeKind.Utc);
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                // Update timestamp on modification
                entry.Entity.UpdatedAt = DateTime.UtcNow;

                // Ensure LastLoginAt is UTC if set
                if (entry.Entity.LastLoginAt.HasValue && entry.Entity.LastLoginAt.Value.Kind != DateTimeKind.Utc)
                {
                    entry.Entity.LastLoginAt = DateTime.SpecifyKind(entry.Entity.LastLoginAt.Value, DateTimeKind.Utc);
                }
            }
        }
    }
}