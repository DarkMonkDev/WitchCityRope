using Microsoft.EntityFrameworkCore;
using WitchCityRope.Core.Entities;
using WitchCityRope.Infrastructure.Data.Configurations;

namespace WitchCityRope.Infrastructure.Data
{
    /// <summary>
    /// Entity Framework Core database context for WitchCityRope
    /// </summary>
    public class WitchCityRopeDbContext : DbContext
    {
        public WitchCityRopeDbContext(DbContextOptions<WitchCityRopeDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Registration> Registrations { get; set; }
        public DbSet<RSVP> RSVPs { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<VettingApplication> VettingApplications { get; set; }
        public DbSet<IncidentReport> IncidentReports { get; set; }
        public DbSet<UserAuthentication> UserAuthentications { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        
        // Event Session Matrix entities
        public DbSet<EventSession> EventSessions { get; set; }
        public DbSet<EventTicketType> EventTicketTypes { get; set; }
        public DbSet<EventTicketTypeSession> EventTicketTypeSessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply configurations
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new EventConfiguration());
            modelBuilder.ApplyConfiguration(new RegistrationConfiguration());
            modelBuilder.ApplyConfiguration(new RSVPConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentConfiguration());
            modelBuilder.ApplyConfiguration(new VettingApplicationConfiguration());
            modelBuilder.ApplyConfiguration(new IncidentReportConfiguration());
            modelBuilder.ApplyConfiguration(new UserAuthenticationConfiguration());
            modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
            
            // Event Session Matrix configurations
            modelBuilder.ApplyConfiguration(new EventSessionConfiguration());
            modelBuilder.ApplyConfiguration(new EventTicketTypeConfiguration());
            modelBuilder.ApplyConfiguration(new EventTicketTypeSessionConfiguration());
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
                .Where(e => e.Entity is User || e.Entity is Event || e.Entity is Registration || e.Entity is RSVP ||
                           e.Entity is Payment || e.Entity is VettingApplication || e.Entity is IncidentReport ||
                           e.Entity is UserAuthentication || e.Entity is RefreshToken || 
                           e.Entity is EventSession || e.Entity is EventTicketType || e.Entity is EventTicketTypeSession);

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Modified)
                {
                    // Update the UpdatedAt property
                    if (entry.Entity.GetType().GetProperty("UpdatedAt") != null)
                    {
                        entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                    }
                }
            }
        }
    }
}