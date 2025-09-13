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

    /// <summary>
    /// Sessions table for event sessions
    /// </summary>
    public DbSet<Session> Sessions { get; set; }

    /// <summary>
    /// TicketTypes table for event ticket types
    /// </summary>
    public DbSet<TicketType> TicketTypes { get; set; }

    /// <summary>
    /// TicketPurchases table for ticket purchases and RSVPs
    /// </summary>
    public DbSet<TicketPurchase> TicketPurchases { get; set; }

    /// <summary>
    /// VolunteerPositions table for event volunteer opportunities
    /// </summary>
    public DbSet<VolunteerPosition> VolunteerPositions { get; set; }

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

            // Navigation properties
            entity.HasMany(e => e.TicketTypes)
                  .WithOne(t => t.Event)
                  .HasForeignKey(t => t.EventId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Sessions)
                  .WithOne(s => s.Event)
                  .HasForeignKey(s => s.EventId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Configure many-to-many relationship with organizers/teachers
            entity.HasMany(e => e.Organizers)
                  .WithMany()
                  .UsingEntity<Dictionary<string, object>>(
                      "EventOrganizers",
                      j => j.HasOne<ApplicationUser>().WithMany().HasForeignKey("UserId"),
                      j => j.HasOne<Event>().WithMany().HasForeignKey("EventId"));
        });

        // Session entity configuration
        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("Sessions", "public");
            entity.HasKey(s => s.Id);

            entity.Property(s => s.SessionCode)
                  .IsRequired()
                  .HasMaxLength(20);

            entity.Property(s => s.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(s => s.StartTime)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            entity.Property(s => s.EndTime)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            entity.Property(s => s.CreatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            entity.Property(s => s.UpdatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            // Foreign key to Event
            entity.HasOne(s => s.Event)
                  .WithMany(e => e.Sessions)
                  .HasForeignKey(s => s.EventId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(s => s.EventId)
                  .HasDatabaseName("IX_Sessions_EventId");

            entity.HasIndex(s => new { s.EventId, s.SessionCode })
                  .IsUnique()
                  .HasDatabaseName("IX_Sessions_EventId_SessionCode");
        });

        // TicketType entity configuration
        modelBuilder.Entity<TicketType>(entity =>
        {
            entity.ToTable("TicketTypes", "public");
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Name)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(t => t.Description)
                  .HasMaxLength(500);

            entity.Property(t => t.Price)
                  .IsRequired()
                  .HasColumnType("decimal(10,2)");

            entity.Property(t => t.CreatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            entity.Property(t => t.UpdatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            // Foreign keys
            entity.HasOne(t => t.Event)
                  .WithMany(e => e.TicketTypes)
                  .HasForeignKey(t => t.EventId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.Session)
                  .WithMany(s => s.TicketTypes)
                  .HasForeignKey(t => t.SessionId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            entity.HasIndex(t => t.EventId)
                  .HasDatabaseName("IX_TicketTypes_EventId");

            entity.HasIndex(t => t.SessionId)
                  .HasDatabaseName("IX_TicketTypes_SessionId");
        });

        // TicketPurchase entity configuration
        modelBuilder.Entity<TicketPurchase>(entity =>
        {
            entity.ToTable("TicketPurchases", "public");
            entity.HasKey(p => p.Id);

            entity.Property(p => p.PurchaseDate)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            entity.Property(p => p.TotalPrice)
                  .IsRequired()
                  .HasColumnType("decimal(10,2)");

            entity.Property(p => p.PaymentStatus)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(p => p.PaymentMethod)
                  .HasMaxLength(50);

            entity.Property(p => p.PaymentReference)
                  .HasMaxLength(200);

            entity.Property(p => p.Notes)
                  .HasMaxLength(1000);

            entity.Property(p => p.CreatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            entity.Property(p => p.UpdatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            // Foreign keys
            entity.HasOne(p => p.TicketType)
                  .WithMany(t => t.Purchases)
                  .HasForeignKey(p => p.TicketTypeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.User)
                  .WithMany()
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(p => p.TicketTypeId)
                  .HasDatabaseName("IX_TicketPurchases_TicketTypeId");

            entity.HasIndex(p => p.UserId)
                  .HasDatabaseName("IX_TicketPurchases_UserId");

            entity.HasIndex(p => p.PaymentStatus)
                  .HasDatabaseName("IX_TicketPurchases_PaymentStatus");
        });

        // VolunteerPosition entity configuration
        modelBuilder.Entity<VolunteerPosition>(entity =>
        {
            entity.ToTable("VolunteerPositions", "public");
            entity.HasKey(v => v.Id);

            entity.Property(v => v.Title)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(v => v.Description)
                  .IsRequired()
                  .HasMaxLength(1000);

            entity.Property(v => v.Requirements)
                  .HasMaxLength(500);

            entity.Property(v => v.CreatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            entity.Property(v => v.UpdatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            // Foreign keys
            entity.HasOne(v => v.Event)
                  .WithMany()
                  .HasForeignKey(v => v.EventId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.Session)
                  .WithMany()
                  .HasForeignKey(v => v.SessionId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            entity.HasIndex(v => v.EventId)
                  .HasDatabaseName("IX_VolunteerPositions_EventId");

            entity.HasIndex(v => v.SessionId)
                  .HasDatabaseName("IX_VolunteerPositions_SessionId");
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

        // Handle Session entities
        var sessionEntries = ChangeTracker.Entries<Session>();
        foreach (var entry in sessionEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;

                if (entry.Entity.StartTime.Kind != DateTimeKind.Utc)
                    entry.Entity.StartTime = DateTime.SpecifyKind(entry.Entity.StartTime, DateTimeKind.Utc);
                if (entry.Entity.EndTime.Kind != DateTimeKind.Utc)
                    entry.Entity.EndTime = DateTime.SpecifyKind(entry.Entity.EndTime, DateTimeKind.Utc);
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Handle TicketType entities
        var ticketTypeEntries = ChangeTracker.Entries<TicketType>();
        foreach (var entry in ticketTypeEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Handle TicketPurchase entities
        var purchaseEntries = ChangeTracker.Entries<TicketPurchase>();
        foreach (var entry in purchaseEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;

                if (entry.Entity.PurchaseDate.Kind != DateTimeKind.Utc)
                    entry.Entity.PurchaseDate = DateTime.SpecifyKind(entry.Entity.PurchaseDate, DateTimeKind.Utc);
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Handle VolunteerPosition entities
        var volunteerEntries = ChangeTracker.Entries<VolunteerPosition>();
        foreach (var entry in volunteerEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}