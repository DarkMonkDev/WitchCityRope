using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Features.Safety.Entities;
using WitchCityRope.Api.Features.CheckIn.Entities;
using WitchCityRope.Api.Features.CheckIn.Entities.Configuration;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.Vetting.Entities.Configuration;
using WitchCityRope.Api.Features.Payments.Entities;
using WitchCityRope.Api.Features.Payments.Data;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Participation.Data;

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

    /// <summary>
    /// SafetyIncidents table for safety incident reporting
    /// </summary>
    public DbSet<SafetyIncident> SafetyIncidents { get; set; }

    /// <summary>
    /// IncidentAuditLogs table for audit trail
    /// </summary>
    public DbSet<IncidentAuditLog> IncidentAuditLogs { get; set; }

    /// <summary>
    /// IncidentNotifications table for email notifications
    /// </summary>
    public DbSet<IncidentNotification> IncidentNotifications { get; set; }

    /// <summary>
    /// EventAttendees table for event registration and check-in
    /// </summary>
    public DbSet<EventAttendee> EventAttendees { get; set; }

    /// <summary>
    /// CheckIns table for actual check-in records
    /// </summary>
    public DbSet<CheckIn> CheckIns { get; set; }

    /// <summary>
    /// CheckInAuditLogs table for check-in audit trail
    /// </summary>
    public DbSet<CheckInAuditLog> CheckInAuditLogs { get; set; }

    /// <summary>
    /// OfflineSyncQueues table for offline synchronization
    /// </summary>
    public DbSet<OfflineSyncQueue> OfflineSyncQueues { get; set; }

    /// <summary>
    /// VettingApplications table for member vetting applications
    /// </summary>
    public DbSet<VettingApplication> VettingApplications { get; set; }

    /// <summary>
    /// VettingAuditLogs table for simplified audit trail
    /// </summary>
    public DbSet<VettingAuditLog> VettingAuditLogs { get; set; }

    /// <summary>
    /// VettingEmailTemplates table for admin-manageable email templates
    /// </summary>
    public DbSet<VettingEmailTemplate> VettingEmailTemplates { get; set; }

    /// <summary>
    /// VettingEmailLogs table for SendGrid email delivery tracking
    /// </summary>
    public DbSet<VettingEmailLog> VettingEmailLogs { get; set; }

    /// <summary>
    /// VettingNotifications table for email notifications
    /// </summary>
    public DbSet<VettingNotification> VettingNotifications { get; set; }

    /// <summary>
    /// VettingBulkOperations table for bulk administrative operations
    /// </summary>
    public DbSet<VettingBulkOperation> VettingBulkOperations { get; set; }

    /// <summary>
    /// VettingBulkOperationItems table for individual application processing in bulk operations
    /// </summary>
    public DbSet<VettingBulkOperationItem> VettingBulkOperationItems { get; set; }

    /// <summary>
    /// VettingBulkOperationLogs table for detailed bulk operation logging
    /// </summary>
    public DbSet<VettingBulkOperationLog> VettingBulkOperationLogs { get; set; }

    /// <summary>
    /// Payments table for payment processing with sliding scale pricing
    /// </summary>
    public DbSet<Payment> Payments { get; set; }

    /// <summary>
    /// PaymentMethods table for saved payment methods
    /// </summary>
    public DbSet<PaymentMethod> PaymentMethods { get; set; }

    /// <summary>
    /// PaymentRefunds table for refund tracking
    /// </summary>
    public DbSet<PaymentRefund> PaymentRefunds { get; set; }

    /// <summary>
    /// PaymentAuditLog table for comprehensive payment audit trails
    /// </summary>
    public DbSet<PaymentAuditLog> PaymentAuditLog { get; set; }

    /// <summary>
    /// PaymentFailures table for detailed failure tracking
    /// </summary>
    public DbSet<PaymentFailure> PaymentFailures { get; set; }

    /// <summary>
    /// EventParticipations table for RSVP and ticket participation tracking
    /// </summary>
    public DbSet<EventParticipation> EventParticipations { get; set; }

    /// <summary>
    /// ParticipationHistory table for participation audit trails
    /// </summary>
    public DbSet<ParticipationHistory> ParticipationHistory { get; set; }

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

            // Profile fields - Added for profile management
            entity.Property(e => e.FirstName)
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .HasMaxLength(100);

            entity.Property(e => e.Bio)
                .HasColumnType("text");

            entity.Property(e => e.DiscordName)
                .HasMaxLength(100);

            entity.Property(e => e.FetLifeName)
                .HasMaxLength(100);

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

            entity.HasMany(e => e.VolunteerPositions)
                  .WithOne(v => v.Event)
                  .HasForeignKey(v => v.EventId)
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

            // Foreign keys - Event relationship configured in Event entity

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

        // SafetyIncident entity configuration
        modelBuilder.Entity<SafetyIncident>(entity =>
        {
            entity.ToTable("SafetyIncidents", "public");
            entity.HasKey(e => e.Id);

            // String properties
            entity.Property(e => e.ReferenceNumber)
                  .IsRequired()
                  .HasMaxLength(20);

            entity.Property(e => e.Location)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.EncryptedDescription)
                  .IsRequired()
                  .HasColumnType("text");

            entity.Property(e => e.EncryptedInvolvedParties)
                  .HasColumnType("text");

            entity.Property(e => e.EncryptedWitnesses)
                  .HasColumnType("text");

            entity.Property(e => e.EncryptedContactEmail)
                  .HasMaxLength(500);

            entity.Property(e => e.EncryptedContactPhone)
                  .HasMaxLength(200);

            // DateTime properties - CRITICAL: Use timestamptz for PostgreSQL
            entity.Property(e => e.IncidentDate)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            entity.Property(e => e.ReportedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            entity.Property(e => e.CreatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            entity.Property(e => e.UpdatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            // Enum properties
            entity.Property(e => e.Severity)
                  .IsRequired()
                  .HasConversion<int>();

            entity.Property(e => e.Status)
                  .IsRequired()
                  .HasConversion<int>();

            // Indexes
            entity.HasIndex(e => e.ReferenceNumber)
                  .IsUnique()
                  .HasDatabaseName("IX_SafetyIncidents_ReferenceNumber");

            entity.HasIndex(e => e.Severity)
                  .HasDatabaseName("IX_SafetyIncidents_Severity");

            entity.HasIndex(e => e.Status)
                  .HasDatabaseName("IX_SafetyIncidents_Status");

            entity.HasIndex(e => e.ReportedAt)
                  .HasDatabaseName("IX_SafetyIncidents_ReportedAt");

            entity.HasIndex(e => e.ReporterId)
                  .HasDatabaseName("IX_SafetyIncidents_ReporterId")
                  .HasFilter("\"ReporterId\" IS NOT NULL");

            entity.HasIndex(e => new { e.Status, e.Severity, e.ReportedAt })
                  .HasDatabaseName("IX_SafetyIncidents_Status_Severity_ReportedAt");

            // Foreign key relationships
            entity.HasOne(e => e.Reporter)
                  .WithMany()
                  .HasForeignKey(e => e.ReporterId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.AssignedUser)
                  .WithMany()
                  .HasForeignKey(e => e.AssignedTo)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.UpdatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.UpdatedBy)
                  .OnDelete(DeleteBehavior.SetNull);

            // One-to-many relationships
            entity.HasMany(e => e.AuditLogs)
                  .WithOne(a => a.Incident)
                  .HasForeignKey(a => a.IncidentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Notifications)
                  .WithOne(n => n.Incident)
                  .HasForeignKey(n => n.IncidentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // IncidentAuditLog entity configuration
        modelBuilder.Entity<IncidentAuditLog>(entity =>
        {
            entity.ToTable("IncidentAuditLog", "public");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ActionType)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(e => e.ActionDescription)
                  .IsRequired()
                  .HasColumnType("text");

            entity.Property(e => e.OldValues)
                  .HasColumnType("jsonb");

            entity.Property(e => e.NewValues)
                  .HasColumnType("jsonb");

            entity.Property(e => e.IpAddress)
                  .HasMaxLength(45);

            entity.Property(e => e.UserAgent)
                  .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            // Indexes
            entity.HasIndex(e => new { e.IncidentId, e.CreatedAt })
                  .HasDatabaseName("IX_IncidentAuditLog_IncidentId_CreatedAt");

            entity.HasIndex(e => e.ActionType)
                  .HasDatabaseName("IX_IncidentAuditLog_ActionType");

            entity.HasIndex(e => e.CreatedAt)
                  .HasDatabaseName("IX_IncidentAuditLog_CreatedAt");

            // GIN indexes for JSONB columns
            entity.HasIndex(e => e.OldValues)
                  .HasDatabaseName("IX_IncidentAuditLog_OldValues")
                  .HasMethod("gin");

            entity.HasIndex(e => e.NewValues)
                  .HasDatabaseName("IX_IncidentAuditLog_NewValues")
                  .HasMethod("gin");

            // Foreign key relationships
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // IncidentNotification entity configuration
        modelBuilder.Entity<IncidentNotification>(entity =>
        {
            entity.ToTable("IncidentNotifications", "public");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.NotificationType)
                  .IsRequired()
                  .HasMaxLength(20);

            entity.Property(e => e.RecipientType)
                  .IsRequired()
                  .HasMaxLength(20);

            entity.Property(e => e.RecipientEmail)
                  .IsRequired()
                  .HasMaxLength(255);

            entity.Property(e => e.Subject)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.MessageBody)
                  .IsRequired()
                  .HasColumnType("text");

            entity.Property(e => e.Status)
                  .IsRequired()
                  .HasMaxLength(20);

            entity.Property(e => e.ErrorMessage)
                  .HasColumnType("text");

            entity.Property(e => e.SentAt)
                  .HasColumnType("timestamptz");

            entity.Property(e => e.CreatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            entity.Property(e => e.UpdatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            // Indexes
            entity.HasIndex(e => e.IncidentId)
                  .HasDatabaseName("IX_IncidentNotifications_IncidentId");

            entity.HasIndex(e => new { e.Status, e.CreatedAt })
                  .HasDatabaseName("IX_IncidentNotifications_Status_CreatedAt");

            entity.HasIndex(e => e.RecipientType)
                  .HasDatabaseName("IX_IncidentNotifications_RecipientType");

            // Partial index for failed notifications
            entity.HasIndex(e => new { e.CreatedAt, e.RetryCount })
                  .HasDatabaseName("IX_IncidentNotifications_Failed_RetryCount")
                  .HasFilter("\"Status\" = 'Failed' AND \"RetryCount\" < 5");
        });

        // Apply CheckIn System configurations
        modelBuilder.ApplyConfiguration(new EventAttendeeConfiguration());
        modelBuilder.ApplyConfiguration(new CheckInConfiguration());
        modelBuilder.ApplyConfiguration(new CheckInAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new OfflineSyncQueueConfiguration());

        // Apply Vetting System configurations
        modelBuilder.ApplyConfiguration(new VettingApplicationConfiguration());
        modelBuilder.ApplyConfiguration(new VettingAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new VettingEmailTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new VettingEmailLogConfiguration());
        modelBuilder.ApplyConfiguration(new VettingNotificationConfiguration());
        modelBuilder.ApplyConfiguration(new VettingBulkOperationConfiguration());
        modelBuilder.ApplyConfiguration(new VettingBulkOperationItemConfiguration());
        modelBuilder.ApplyConfiguration(new VettingBulkOperationLogConfiguration());

        // Apply Payment System configurations
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentMethodConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentRefundConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentFailureConfiguration());

        // Apply Participation System configurations
        modelBuilder.ApplyConfiguration(new EventParticipationConfiguration());
        modelBuilder.ApplyConfiguration(new ParticipationHistoryConfiguration());
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

        // Handle SafetyIncident entities
        var safetyIncidentEntries = ChangeTracker.Entries<SafetyIncident>();
        foreach (var entry in safetyIncidentEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
                entry.Entity.ReportedAt = DateTime.UtcNow;

                // Ensure IncidentDate is UTC
                if (entry.Entity.IncidentDate.Kind != DateTimeKind.Utc)
                {
                    entry.Entity.IncidentDate = DateTime.SpecifyKind(entry.Entity.IncidentDate, DateTimeKind.Utc);
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Handle IncidentAuditLog entities
        var auditLogEntries = ChangeTracker.Entries<IncidentAuditLog>();
        foreach (var entry in auditLogEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
        }

        // Handle IncidentNotification entities
        var notificationEntries = ChangeTracker.Entries<IncidentNotification>();
        foreach (var entry in notificationEntries)
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

        // Handle EventAttendee entities
        var attendeeEntries = ChangeTracker.Entries<EventAttendee>();
        foreach (var entry in attendeeEntries)
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

        // Handle CheckIn entities
        var checkInEntries = ChangeTracker.Entries<CheckIn>();
        foreach (var entry in checkInEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;

                // Ensure CheckInTime is UTC
                if (entry.Entity.CheckInTime.Kind != DateTimeKind.Utc)
                {
                    entry.Entity.CheckInTime = DateTime.SpecifyKind(entry.Entity.CheckInTime, DateTimeKind.Utc);
                }
            }
        }

        // Handle CheckInAuditLog entities
        var checkInAuditLogEntries = ChangeTracker.Entries<CheckInAuditLog>();
        foreach (var entry in checkInAuditLogEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
        }

        // Handle OfflineSyncQueue entities
        var syncQueueEntries = ChangeTracker.Entries<OfflineSyncQueue>();
        foreach (var entry in syncQueueEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;

                // Ensure LocalTimestamp is UTC
                if (entry.Entity.LocalTimestamp.Kind != DateTimeKind.Utc)
                {
                    entry.Entity.LocalTimestamp = DateTime.SpecifyKind(entry.Entity.LocalTimestamp, DateTimeKind.Utc);
                }
            }
        }

        // Handle VettingApplication entities
        var vettingApplicationEntries = ChangeTracker.Entries<VettingApplication>();
        foreach (var entry in vettingApplicationEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;

                // Ensure all DateTime fields are UTC
                if (entry.Entity.ReviewStartedAt.HasValue && entry.Entity.ReviewStartedAt.Value.Kind != DateTimeKind.Utc)
                {
                    entry.Entity.ReviewStartedAt = DateTime.SpecifyKind(entry.Entity.ReviewStartedAt.Value, DateTimeKind.Utc);
                }
                if (entry.Entity.DecisionMadeAt.HasValue && entry.Entity.DecisionMadeAt.Value.Kind != DateTimeKind.Utc)
                {
                    entry.Entity.DecisionMadeAt = DateTime.SpecifyKind(entry.Entity.DecisionMadeAt.Value, DateTimeKind.Utc);
                }
                if (entry.Entity.InterviewScheduledFor.HasValue && entry.Entity.InterviewScheduledFor.Value.Kind != DateTimeKind.Utc)
                {
                    entry.Entity.InterviewScheduledFor = DateTime.SpecifyKind(entry.Entity.InterviewScheduledFor.Value, DateTimeKind.Utc);
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Deleted entities - VettingReference, VettingReferenceResponse, VettingReviewer, VettingApplicationNote, VettingDecision

        // Handle VettingNotification entities
        var vettingNotificationEntries = ChangeTracker.Entries<VettingNotification>();
        foreach (var entry in vettingNotificationEntries)
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

        // Handle VettingAuditLog entities (replacement for multiple audit log types)
        var vettingAuditLogEntries = ChangeTracker.Entries<VettingAuditLog>();
        foreach (var entry in vettingAuditLogEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.PerformedAt = DateTime.UtcNow;
            }
        }

        // Handle VettingEmailLog entities
        var vettingEmailLogEntries = ChangeTracker.Entries<VettingEmailLog>();
        foreach (var entry in vettingEmailLogEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.SentAt = DateTime.UtcNow;

                // Ensure LastRetryAt is UTC if set
                if (entry.Entity.LastRetryAt.HasValue && entry.Entity.LastRetryAt.Value.Kind != DateTimeKind.Utc)
                {
                    entry.Entity.LastRetryAt = DateTime.SpecifyKind(entry.Entity.LastRetryAt.Value, DateTimeKind.Utc);
                }
            }
        }

        // Handle VettingEmailTemplate entities
        var vettingEmailTemplateEntries = ChangeTracker.Entries<VettingEmailTemplate>();
        foreach (var entry in vettingEmailTemplateEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
                entry.Entity.LastModified = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
                entry.Entity.LastModified = DateTime.UtcNow;
            }
        }

        // Handle Payment entities
        var paymentEntries = ChangeTracker.Entries<Payment>();
        foreach (var entry in paymentEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;

                // Ensure ProcessedAt is UTC if set
                if (entry.Entity.ProcessedAt.HasValue && entry.Entity.ProcessedAt.Value.Kind != DateTimeKind.Utc)
                {
                    entry.Entity.ProcessedAt = DateTime.SpecifyKind(entry.Entity.ProcessedAt.Value, DateTimeKind.Utc);
                }

                // Ensure RefundedAt is UTC if set
                if (entry.Entity.RefundedAt.HasValue && entry.Entity.RefundedAt.Value.Kind != DateTimeKind.Utc)
                {
                    entry.Entity.RefundedAt = DateTime.SpecifyKind(entry.Entity.RefundedAt.Value, DateTimeKind.Utc);
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;

                // Ensure ProcessedAt is UTC if set
                if (entry.Entity.ProcessedAt.HasValue && entry.Entity.ProcessedAt.Value.Kind != DateTimeKind.Utc)
                {
                    entry.Entity.ProcessedAt = DateTime.SpecifyKind(entry.Entity.ProcessedAt.Value, DateTimeKind.Utc);
                }

                // Ensure RefundedAt is UTC if set
                if (entry.Entity.RefundedAt.HasValue && entry.Entity.RefundedAt.Value.Kind != DateTimeKind.Utc)
                {
                    entry.Entity.RefundedAt = DateTime.SpecifyKind(entry.Entity.RefundedAt.Value, DateTimeKind.Utc);
                }
            }
        }

        // Handle PaymentMethod entities
        var paymentMethodEntries = ChangeTracker.Entries<PaymentMethod>();
        foreach (var entry in paymentMethodEntries)
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

        // Handle PaymentRefund entities
        var paymentRefundEntries = ChangeTracker.Entries<PaymentRefund>();
        foreach (var entry in paymentRefundEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.ProcessedAt = DateTime.UtcNow;
            }
        }

        // Handle PaymentAuditLog entities
        var paymentAuditLogEntries = ChangeTracker.Entries<PaymentAuditLog>();
        foreach (var entry in paymentAuditLogEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
        }

        // Handle PaymentFailure entities
        var paymentFailureEntries = ChangeTracker.Entries<PaymentFailure>();
        foreach (var entry in paymentFailureEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.FailedAt = DateTime.UtcNow;
            }
        }

        // Handle EventParticipation entities
        var eventParticipationEntries = ChangeTracker.Entries<EventParticipation>();
        foreach (var entry in eventParticipationEntries)
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

        // Handle ParticipationHistory entities
        var participationHistoryEntries = ChangeTracker.Entries<ParticipationHistory>();
        foreach (var entry in participationHistoryEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
        }
    }
}
