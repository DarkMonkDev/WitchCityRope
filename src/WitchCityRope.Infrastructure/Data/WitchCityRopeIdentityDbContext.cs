using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Core.Entities;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Infrastructure.Data.Configurations;

namespace WitchCityRope.Infrastructure.Data
{
    /// <summary>
    /// Entity Framework Core database context for WitchCityRope with ASP.NET Core Identity support
    /// </summary>
    public class WitchCityRopeIdentityDbContext : IdentityDbContext<WitchCityRopeUser, WitchCityRopeRole, Guid>
    {
        public WitchCityRopeIdentityDbContext(DbContextOptions<WitchCityRopeIdentityDbContext> options)
            : base(options)
        {
        }

        // Custom entities (non-Identity)
        public DbSet<Event> Events { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Rsvp> Rsvps { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<VettingApplication> VettingApplications { get; set; }
        public DbSet<IncidentReport> IncidentReports { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<IncidentReview> IncidentReviews { get; set; }
        public DbSet<IncidentAction> IncidentActions { get; set; }
        public DbSet<ContentPage> ContentPages { get; set; }
        public DbSet<UserNote> UserNotes { get; set; }
        public DbSet<VolunteerTask> VolunteerTasks { get; set; }
        public DbSet<VolunteerAssignment> VolunteerAssignments { get; set; }
        public DbSet<EventEmailTemplate> EventEmailTemplates { get; set; }
        public DbSet<Registration> Registrations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Core.Entities.User has been removed - using WitchCityRopeUser only

            // Configure Identity tables to use custom names and schema
            modelBuilder.Entity<WitchCityRopeUser>().ToTable("Users", "auth");
            modelBuilder.Entity<WitchCityRopeRole>().ToTable("Roles", "auth");
            modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles", "auth");
            modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims", "auth");
            modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins", "auth");
            modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims", "auth");
            modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens", "auth");

            // Configure WitchCityRopeUser entity
            modelBuilder.Entity<WitchCityRopeUser>(entity =>
            {
                // Configure SceneName value object
                entity.Ignore(u => u.SceneName); // Use SceneNameValue instead for storage
                
                // Configure EmailAddress value object
                entity.Ignore(u => u.EmailAddress); // Use Email property from IdentityUser

                // Note: Email is already provided by IdentityUser base class
                // The EmailAddress value object might not be needed
                // entity.OwnsOne(u => u.EmailAddress, email =>
                // {
                //     email.Property(e => e.Value)
                //         .HasColumnName("EmailValue")
                //         .HasMaxLength(254)
                //         .IsRequired();

                //     email.Property(e => e.DisplayValue)
                //         .HasColumnName("EmailDisplayValue")
                //         .HasMaxLength(254)
                //         .IsRequired();
                // });

                // Configure properties
                entity.Property(u => u.EncryptedLegalName)
                    .IsRequired()
                    .HasMaxLength(500);
                    
                entity.Property(u => u.SceneNameValue)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("SceneName");
                    
                entity.HasIndex(u => u.SceneNameValue)
                    .IsUnique()
                    .HasDatabaseName("IX_Users_SceneName");

                entity.Property(u => u.DateOfBirth)
                    .IsRequired();

                entity.Property(u => u.Role)
                    .IsRequired()
                    .HasConversion<string>();

                entity.Property(u => u.PronouncedName)
                    .HasMaxLength(100);

                entity.Property(u => u.Pronouns)
                    .HasMaxLength(50);

                entity.Property(u => u.CreatedAt)
                    .IsRequired();

                entity.Property(u => u.UpdatedAt)
                    .IsRequired();

                // Configure indexes
                // Note: Email is inherited from IdentityUser, no need for separate index

                entity.HasIndex(u => u.IsActive)
                    .HasDatabaseName("IX_Users_IsActive");

                entity.HasIndex(u => u.IsVetted)
                    .HasDatabaseName("IX_Users_IsVetted");

                entity.HasIndex(u => u.Role)
                    .HasDatabaseName("IX_Users_Role");

                // Configure relationships
                entity.HasMany(u => u.RefreshTokens)
                    .WithOne()
                    .HasForeignKey(rt => rt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasMany(u => u.Registrations)
                    .WithOne()
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure WitchCityRopeRole entity
            modelBuilder.Entity<WitchCityRopeRole>(entity =>
            {
                entity.Property(r => r.Description)
                    .HasMaxLength(500);

                entity.Property(r => r.CreatedAt)
                    .IsRequired();

                entity.Property(r => r.UpdatedAt)
                    .IsRequired();

                entity.HasIndex(r => r.IsActive)
                    .HasDatabaseName("IX_Roles_IsActive");

                entity.HasIndex(r => r.Priority)
                    .HasDatabaseName("IX_Roles_Priority");
            });

            // Apply other configurations
            modelBuilder.ApplyConfiguration(new EventConfiguration());
            modelBuilder.ApplyConfiguration(new TicketConfiguration());
            modelBuilder.ApplyConfiguration(new RsvpConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentConfiguration());
            modelBuilder.ApplyConfiguration(new VettingApplicationConfiguration());
            modelBuilder.ApplyConfiguration(new IncidentReportConfiguration());
            modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
            modelBuilder.ApplyConfiguration(new ContentPageConfiguration());
            modelBuilder.ApplyConfiguration(new UserNoteConfiguration());
            modelBuilder.ApplyConfiguration(new VolunteerTaskConfiguration());
            modelBuilder.ApplyConfiguration(new VolunteerAssignmentConfiguration());
            modelBuilder.ApplyConfiguration(new EventEmailTemplateConfiguration());
            modelBuilder.ApplyConfiguration(new RegistrationConfiguration());

            // Seed default roles
            SeedRoles(modelBuilder);
        }

        private void SeedRoles(ModelBuilder modelBuilder)
        {
            var roles = new[]
            {
                new 
                { 
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "Attendee",
                    NormalizedName = "ATTENDEE",
                    Description = "Standard event attendee",
                    Priority = 0,
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    ConcurrencyStamp = "11111111-1111-1111-1111-111111111111"
                },
                new
                { 
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "Member",
                    NormalizedName = "MEMBER",
                    Description = "Verified community member with additional privileges",
                    Priority = 1,
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    ConcurrencyStamp = "22222222-2222-2222-2222-222222222222"
                },
                new
                { 
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "Organizer",
                    NormalizedName = "ORGANIZER",
                    Description = "Event organizer who can create and manage events",
                    Priority = 2,
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    ConcurrencyStamp = "33333333-3333-3333-3333-333333333333"
                },
                new
                { 
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Name = "Moderator",
                    NormalizedName = "MODERATOR",
                    Description = "Community moderator who can review incidents and vetting",
                    Priority = 3,
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    ConcurrencyStamp = "44444444-4444-4444-4444-444444444444"
                },
                new
                { 
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Name = "Administrator",
                    NormalizedName = "ADMINISTRATOR",
                    Description = "System administrator with full access",
                    Priority = 4,
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    ConcurrencyStamp = "55555555-5555-5555-5555-555555555555"
                }
            };

            modelBuilder.Entity<WitchCityRopeRole>().HasData(roles);
        }

        public override int SaveChanges()
        {
            UpdateAuditFields();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateAuditFields()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified || e.State == EntityState.Added);

            foreach (var entry in entries)
            {
                // Convert all DateTime properties to UTC if they're not already
                foreach (var property in entry.Properties)
                {
                    if (property.CurrentValue is DateTime dateTime)
                    {
                        if (dateTime.Kind == DateTimeKind.Unspecified)
                        {
                            property.CurrentValue = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                        }
                        else if (dateTime.Kind == DateTimeKind.Local)
                        {
                            property.CurrentValue = dateTime.ToUniversalTime();
                        }
                    }
                }

                if (entry.Entity is WitchCityRopeUser user)
                {
                    if (entry.State == EntityState.Modified)
                    {
                        user.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (entry.Entity is WitchCityRopeRole role)
                {
                    if (entry.State == EntityState.Modified)
                    {
                        role.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (entry.Entity.GetType().GetProperty("UpdatedAt") != null)
                {
                    entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                }
            }
        }
    }
}