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
using WitchCityRope.Api.Features.Payments.Configuration;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Participation.Configuration;
using WitchCityRope.Api.Features.Cms.Entities;
using WitchCityRope.Api.Features.Cms.Configuration;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.EmailTemplates.Entities.Configuration;

namespace WitchCityRope.Api.Data;

/// <summary>
/// Entity Framework Core DbContext for PostgreSQL with ASP.NET Core Identity
/// Configured for vertical slice proof-of-concept with authentication
///
/// <para><strong>MIGRATION CONFIGURATION:</strong></para>
/// <para>
/// Migrations are stored in the default location: /apps/api/Migrations/
/// This is EF Core's default behavior when no --output-dir flag is specified.
/// </para>
///
/// <para><strong>CRITICAL: DO NOT use the --output-dir flag when creating migrations!</strong></para>
/// <para>
/// Using --output-dir creates multiple migration directories which causes confusion
/// and can result in empty migration files being generated in the wrong location.
/// The correct migration directory is determined by the .csproj file location.
/// </para>
///
/// <para><strong>IMPORTANT:</strong></para>
/// <para>
/// ALWAYS create migrations in /apps/api/Migrations/ directory.
/// DO NOT create migrations in /apps/api/Data/Migrations/ directory.
/// Use: dotnet ef migrations add MigrationName (from /apps/api directory)
/// </para>
///
/// <para><strong>Creating New Migrations:</strong></para>
/// <code>
/// # From /apps/api directory (where .csproj is located):
/// dotnet ef migrations add YourMigrationName
///
/// # This will create migration files in:
/// # /apps/api/Migrations/YYYYMMDDHHMMSS_YourMigrationName.cs
/// # /apps/api/Migrations/ApplicationDbContextModelSnapshot.cs
/// </code>
///
/// <para><strong>Initial Migration State:</strong></para>
/// <para>
/// The project has been reset to a single initial migration (InitialMigration)
/// containing the complete schema. This 2674-line migration represents the entire
/// database structure as of October 2025. Future migrations will only contain
/// incremental changes from this baseline.
/// </para>
///
/// <para><strong>Migration Namespace:</strong></para>
/// <para>
/// All migrations use the namespace: WitchCityRope.Api.Migrations
/// This namespace is automatically applied by EF Core based on the project structure.
/// </para>
///
/// <para><strong>Database Initialization:</strong></para>
/// <para>
/// On startup, DatabaseInitializationService.cs applies pending migrations using
/// context.Database.MigrateAsync(). This creates the __EFMigrationsHistory table
/// to track which migrations have been applied. DO NOT use EnsureCreated() as it
/// bypasses the migration system and creates schema without migration tracking.
/// </para>
///
/// <para><strong>Resetting Migrations (if needed):</strong></para>
/// <code>
/// # 1. Delete all files in /apps/api/Migrations/ directory
/// # 2. Drop the database (or just the __EFMigrationsHistory table)
/// # 3. Create a fresh initial migration:
/// dotnet ef migrations add InitialMigration
///
/// # 4. Verify the migration contains the full schema (should be ~2500+ lines)
/// # 5. Apply the migration:
/// dotnet ef database update
/// </code>
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
    /// Venues table for venue management
    /// </summary>
    public DbSet<Venue> Venues { get; set; }

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
    /// VolunteerSignups table for tracking user volunteer sign-ups
    /// </summary>
    public DbSet<VolunteerSignup> VolunteerSignups { get; set; }

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
    /// IncidentNotes table for investigation notes
    /// </summary>
    public DbSet<IncidentNote> IncidentNotes => Set<IncidentNote>();

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
    /// CheckInSessionTokens table for kiosk mode session tokens
    /// </summary>
    public DbSet<CheckInSessionToken> CheckInSessionTokens { get; set; }

    /// <summary>
    /// VettingApplications table for member vetting applications
    /// </summary>
    public DbSet<VettingApplication> VettingApplications { get; set; }

    /// <summary>
    /// VettingAuditLogs table for simplified audit trail
    /// </summary>
    public DbSet<VettingAuditLog> VettingAuditLogs { get; set; }

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
    /// Settings table for application-wide configuration
    /// </summary>
    public DbSet<WitchCityRope.Api.Core.Entities.Setting> Settings { get; set; }

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
    /// EventAttendances table for RSVP and ticket attendance tracking
    /// </summary>
    public DbSet<EventAttendance> EventAttendances { get; set; }

    /// <summary>
    /// AttendanceHistory table for attendance audit trails
    /// </summary>
    public DbSet<AttendanceHistory> AttendanceHistory { get; set; }

    /// <summary>
    /// ContentPages table for CMS content management
    /// </summary>
    public DbSet<ContentPage> ContentPages => Set<ContentPage>();

    /// <summary>
    /// ContentRevisions table for CMS content revision history
    /// </summary>
    public DbSet<ContentRevision> ContentRevisions => Set<ContentRevision>();

    /// <summary>
    /// UserNotes table for unified user notes system (vetting, general, administrative, status changes)
    /// </summary>
    public DbSet<WitchCityRope.Api.Data.Entities.UserNote> UserNotes => Set<WitchCityRope.Api.Data.Entities.UserNote>();

    /// <summary>
    /// GlobalEmailTemplates table for global email templates across all categories
    /// </summary>
    public DbSet<GlobalEmailTemplate> GlobalEmailTemplates { get; set; }

    /// <summary>
    /// EventEmailTemplates table for event-specific email template overrides
    /// </summary>
    public DbSet<EventEmailTemplate> EventEmailTemplates { get; set; }

    /// <summary>
    /// SentAdHocEmails table for ad-hoc email audit trail
    /// </summary>
    public DbSet<SentAdHocEmail> SentAdHocEmails { get; set; }

    /// <summary>
    /// AdHocEmailTemplates table for saved ad-hoc templates
    /// </summary>
    public DbSet<AdHocEmailTemplate> AdHocEmailTemplates { get; set; }

    /// <summary>
    /// EmailTriggerLogs table for automated email audit trail
    /// </summary>
    public DbSet<EmailTriggerLog> EmailTriggerLogs { get; set; }

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
            // Override ASP.NET Identity defaults for Email field size
            entity.Property(e => e.Email)
                .HasMaxLength(100);

            entity.Property(e => e.SceneName)
                .IsRequired()
                .HasMaxLength(50);

            // Profile fields - Added for profile management
            entity.Property(e => e.FirstName)
                .HasMaxLength(50);

            entity.Property(e => e.LastName)
                .HasMaxLength(50);

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

            // Terms of Service fields for legal compliance
            entity.Property(e => e.TermsOfServiceAccepted)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.TermsOfServiceAcceptedAt)
                .HasColumnType("timestamptz");

            // Indexes to match existing schema
            // Scene names can be duplicated - email is the unique identifier
            entity.HasIndex(e => e.SceneName)
                .HasDatabaseName("IX_Users_SceneName");

            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_Users_IsActive");

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

            entity.Property(e => e.IsPublished)
                  .IsRequired();

            entity.Property(e => e.CreatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            entity.Property(e => e.UpdatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");

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

            // Configure relationship with Venue (optional foreign key)
            entity.HasOne(e => e.Venue)
                  .WithMany(v => v.Events)
                  .HasForeignKey(e => e.VenueId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Venue entity configuration
        modelBuilder.Entity<Venue>(entity =>
        {
            entity.ToTable("Venues", "public");
            entity.HasKey(v => v.Id);

            // Name: required, max 100, unique case-insensitive
            entity.Property(v => v.Name)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.HasIndex(v => v.Name)
                  .IsUnique()
                  .HasDatabaseName("IX_Venues_Name");

            // Directions: optional, max 500
            entity.Property(v => v.Directions)
                  .HasMaxLength(500);

            // Notes: optional, max 1000
            entity.Property(v => v.Notes)
                  .HasMaxLength(1000);

            // Location: optional, max 100
            entity.Property(v => v.Location)
                  .HasMaxLength(100);

            // IsActive: required, default true
            entity.Property(v => v.IsActive)
                  .IsRequired()
                  .HasDefaultValue(true);

            entity.HasIndex(v => v.IsActive)
                  .HasDatabaseName("IX_Venues_IsActive");

            // CreatedAt/UpdatedAt: UTC timestamps
            entity.Property(v => v.CreatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            entity.Property(v => v.UpdatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            // Navigation property to Events
            entity.HasMany(v => v.Events)
                  .WithOne(e => e.Venue)
                  .HasForeignKey(e => e.VenueId)
                  .OnDelete(DeleteBehavior.SetNull);  // Preserve events if venue soft-deleted
        });

        // Session entity configuration
        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("Sessions", "public");
            entity.HasKey(s => s.Id);

            entity.Property(s => s.SessionCode)
                  .IsRequired()
                  .HasMaxLength(10);

            entity.Property(s => s.Name)
                  .IsRequired()
                  .HasMaxLength(100);

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

            entity.Property(t => t.PricingType)
                  .IsRequired()
                  .HasConversion<string>();

            entity.Property(t => t.Price)
                  .HasColumnType("decimal(10,2)");

            entity.Property(t => t.MinPrice)
                  .HasColumnType("decimal(10,2)");

            entity.Property(t => t.MaxPrice)
                  .HasColumnType("decimal(10,2)");

            entity.Property(t => t.DefaultPrice)
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

            // Many-to-many relationship with Sessions
            entity.HasMany(t => t.Sessions)
                  .WithMany(s => s.TicketTypes)
                  .UsingEntity<Dictionary<string, object>>(
                      "TicketTypeSessions",
                      j => j.HasOne<Session>().WithMany().HasForeignKey("SessionId").OnDelete(DeleteBehavior.Cascade),
                      j => j.HasOne<TicketType>().WithMany().HasForeignKey("TicketTypeId").OnDelete(DeleteBehavior.Cascade),
                      j =>
                      {
                          j.HasKey("TicketTypeId", "SessionId");
                          j.ToTable("TicketTypeSessions");
                      });

            // Indexes
            entity.HasIndex(t => t.EventId)
                  .HasDatabaseName("IX_TicketTypes_EventId");
        });

        // TicketPurchase entity configuration
        modelBuilder.Entity<TicketPurchase>(entity =>
        {
            entity.ToTable("TicketPurchases", "public", t =>
            {
                // Check constraint for Notes max length
                t.HasCheckConstraint(
                    "CHK_TicketPurchases_Notes_MaxLength",
                    "LENGTH(\"Notes\") <= 500 OR \"Notes\" IS NULL"
                );
            });
            entity.HasKey(p => p.Id);

            entity.Property(p => p.PurchaseDate)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            entity.Property(p => p.TotalPrice)
                  .IsRequired()
                  .HasColumnType("decimal(10,2)");

            entity.Property(p => p.PaymentStatus)
                  .IsRequired()
                  .HasMaxLength(20);

            entity.Property(p => p.PaymentMethod)
                  .HasMaxLength(20);

            entity.Property(p => p.PaymentReference)
                  .HasMaxLength(50);

            entity.Property(p => p.Notes)
                  .HasMaxLength(500);

            // PayPal encrypted fields configuration
            entity.Property(p => p.EncryptedPayPalOrderId)
                  .HasMaxLength(500);

            entity.Property(p => p.EncryptedPayPalPayerId)
                  .HasMaxLength(500);

            entity.Property(p => p.EncryptedPayPalCaptureId)
                  .HasMaxLength(500);

            // Sliding scale and idempotency fields
            entity.Property(p => p.SlidingScalePercentage)
                  .HasColumnType("decimal(5,2)")
                  .HasDefaultValue(0.00m);

            entity.Property(p => p.IdempotencyKey)
                  .HasMaxLength(50);

            entity.Property(p => p.ProcessedAt)
                  .HasColumnType("timestamptz");

            // RecordedByStaffId configuration
            entity.Property(p => p.RecordedByStaffId)
                  .IsRequired(false);  // Nullable

            // Event Waiver fields for legal compliance
            entity.Property(p => p.EventWaiverAccepted)
                  .IsRequired()
                  .HasDefaultValue(false);

            entity.Property(p => p.EventWaiverAcceptedAt)
                  .HasColumnType("timestamptz");

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

            // RecordedByStaff relationship
            entity.HasOne(p => p.RecordedByStaff)
                  .WithMany()
                  .HasForeignKey(p => p.RecordedByStaffId)
                  .OnDelete(DeleteBehavior.SetNull);  // Preserve purchase if staff deleted

            // Indexes
            entity.HasIndex(p => p.TicketTypeId)
                  .HasDatabaseName("IX_TicketPurchases_TicketTypeId");

            entity.HasIndex(p => p.UserId)
                  .HasDatabaseName("IX_TicketPurchases_UserId");

            entity.HasIndex(p => p.PaymentStatus)
                  .HasDatabaseName("IX_TicketPurchases_PaymentStatus");

            // Partial index for door purchases audit trail
            entity.HasIndex(p => p.RecordedByStaffId)
                  .HasDatabaseName("IX_TicketPurchases_RecordedByStaffId")
                  .HasFilter("\"RecordedByStaffId\" IS NOT NULL");
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
                  .HasMaxLength(30);

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

            entity.Property(e => e.EncryptedContactName)
                  .HasMaxLength(500);

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
            entity.Property(e => e.Status)
                  .IsRequired()
                  .HasConversion<int>();

            // Indexes
            entity.HasIndex(e => e.ReferenceNumber)
                  .IsUnique()
                  .HasDatabaseName("IX_SafetyIncidents_ReferenceNumber");

            entity.HasIndex(e => e.Status)
                  .HasDatabaseName("IX_SafetyIncidents_Status");

            entity.HasIndex(e => e.ReportedAt)
                  .HasDatabaseName("IX_SafetyIncidents_ReportedAt");

            entity.HasIndex(e => e.ReporterId)
                  .HasDatabaseName("IX_SafetyIncidents_ReporterId")
                  .HasFilter("\"ReporterId\" IS NOT NULL");

            entity.HasIndex(e => new { e.Status, e.ReportedAt })
                  .HasDatabaseName("IX_SafetyIncidents_Status_ReportedAt");

            // NEW: Google Drive fields
            entity.Property(e => e.GoogleDriveFolderUrl)
                  .HasMaxLength(500);

            entity.Property(e => e.GoogleDriveFinalReportUrl)
                  .HasMaxLength(500);

            // Foreign key relationships
            entity.HasOne(e => e.Reporter)
                  .WithMany()
                  .HasForeignKey(e => e.ReporterId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.AssignedUser)
                  .WithMany()
                  .HasForeignKey(e => e.AssignedTo)
                  .OnDelete(DeleteBehavior.SetNull);

            // NEW: Coordinator relationship
            entity.HasOne(e => e.Coordinator)
                  .WithMany()
                  .HasForeignKey(e => e.CoordinatorId)
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

            // NEW: Notes relationship
            entity.HasMany(e => e.Notes)
                  .WithOne(n => n.Incident)
                  .HasForeignKey(n => n.IncidentId)
                  .OnDelete(DeleteBehavior.Cascade);

            // NEW: Coordinator workload index
            entity.HasIndex(e => new { e.CoordinatorId, e.Status })
                  .HasDatabaseName("IX_SafetyIncidents_CoordinatorId_Status");

            // NEW: Unassigned queue partial index
            entity.HasIndex(e => new { e.Status, e.CoordinatorId })
                  .HasDatabaseName("IX_SafetyIncidents_Status_CoordinatorId")
                  .HasFilter("\"CoordinatorId\" IS NULL");
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

        // IncidentNote entity configuration
        modelBuilder.Entity<IncidentNote>(entity =>
        {
            entity.ToTable("IncidentNotes", "public");
            entity.HasKey(e => e.Id);

            // Required fields
            entity.Property(e => e.IncidentId)
                  .IsRequired();

            entity.Property(e => e.Content)
                  .IsRequired()
                  .HasColumnType("text");

            entity.Property(e => e.Type)
                  .IsRequired()
                  .HasConversion<int>();

            // Optional fields
            entity.Property(e => e.Tags)
                  .HasMaxLength(200);

            // DateTime fields (UTC timestamptz)
            entity.Property(e => e.CreatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            entity.Property(e => e.UpdatedAt)
                  .HasColumnType("timestamptz");

            // Relationships
            entity.HasOne(e => e.Incident)
                  .WithMany(i => i.Notes)
                  .HasForeignKey(e => e.IncidentId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("FK_IncidentNotes_Incidents_IncidentId");

            entity.HasOne(e => e.Author)
                  .WithMany()
                  .HasForeignKey(e => e.AuthorId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("FK_IncidentNotes_Users_AuthorId");

            // Indexes
            entity.HasIndex(e => new { e.IncidentId, e.CreatedAt })
                  .HasDatabaseName("IX_IncidentNotes_IncidentId_CreatedAt")
                  .IsDescending(false, true);

            entity.HasIndex(e => e.AuthorId)
                  .HasDatabaseName("IX_IncidentNotes_AuthorId")
                  .HasFilter("\"AuthorId\" IS NOT NULL");

            entity.HasIndex(e => e.Type)
                  .HasDatabaseName("IX_IncidentNotes_Type");

            // Check constraints
            entity.HasCheckConstraint(
                "CHK_IncidentNotes_Type",
                "\"Type\" IN (1, 2)"
            );

            entity.HasCheckConstraint(
                "CHK_IncidentNotes_Content_NotEmpty",
                "LENGTH(TRIM(\"Content\")) > 0"
            );
        });

        // Apply CheckIn System configurations
        modelBuilder.ApplyConfiguration(new EventAttendeeConfiguration());
        modelBuilder.ApplyConfiguration(new CheckInConfiguration());
        modelBuilder.ApplyConfiguration(new CheckInAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new OfflineSyncQueueConfiguration());
        modelBuilder.ApplyConfiguration(new CheckInSessionTokenConfiguration());

        // Apply Vetting System configurations
        modelBuilder.ApplyConfiguration(new VettingApplicationConfiguration());
        modelBuilder.ApplyConfiguration(new VettingAuditLogConfiguration());
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

        // Apply Attendance System configurations
        modelBuilder.ApplyConfiguration(new EventAttendanceConfiguration());
        modelBuilder.ApplyConfiguration(new AttendanceHistoryConfiguration());

        // Apply CMS configurations
        modelBuilder.ApplyConfiguration(new ContentPageConfiguration());
        modelBuilder.ApplyConfiguration(new ContentRevisionConfiguration());

        // Apply Email Templates configurations
        modelBuilder.ApplyConfiguration(new GlobalEmailTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new EventEmailTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new SentAdHocEmailConfiguration());
        modelBuilder.ApplyConfiguration(new AdHocEmailTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new EmailTriggerLogConfiguration());

        // Settings entity configuration
        modelBuilder.Entity<WitchCityRope.Api.Core.Entities.Setting>(entity =>
        {
            entity.ToTable("Settings", "public");
            entity.HasKey(s => s.Id);

            // Unique constraint on Key to prevent duplicate settings
            entity.HasIndex(s => s.Key)
                  .IsUnique()
                  .HasDatabaseName("IX_Settings_Key");

            entity.Property(s => s.Key)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(s => s.Value)
                  .IsRequired()
                  .HasMaxLength(500);

            entity.Property(s => s.Description)
                  .HasMaxLength(500);

            entity.Property(s => s.CreatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            entity.Property(s => s.UpdatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");
        });

        // UserNote entity configuration
        modelBuilder.Entity<WitchCityRope.Api.Data.Entities.UserNote>(entity =>
        {
            entity.ToTable("UserNotes", "public");
            entity.HasKey(un => un.Id);

            // Required fields
            entity.Property(un => un.UserId)
                  .IsRequired();

            entity.Property(un => un.Content)
                  .IsRequired()
                  .HasMaxLength(5000);

            entity.Property(un => un.NoteType)
                  .IsRequired()
                  .HasMaxLength(50);

            // Optional fields
            entity.Property(un => un.AuthorId);

            entity.Property(un => un.IsArchived)
                  .IsRequired()
                  .HasDefaultValue(false);

            // DateTime fields
            entity.Property(un => un.CreatedAt)
                  .IsRequired()
                  .HasColumnType("timestamptz");

            // Relationships
            entity.HasOne(un => un.User)
                  .WithMany()
                  .HasForeignKey(un => un.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(un => un.Author)
                  .WithMany()
                  .HasForeignKey(un => un.AuthorId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            entity.HasIndex(un => un.UserId)
                  .HasDatabaseName("IX_UserNotes_UserId");

            entity.HasIndex(un => un.NoteType)
                  .HasDatabaseName("IX_UserNotes_NoteType");

            entity.HasIndex(un => new { un.UserId, un.CreatedAt })
                  .HasDatabaseName("IX_UserNotes_UserId_CreatedAt")
                  .IsDescending(false, true); // User ASC, CreatedAt DESC

            entity.HasIndex(un => un.AuthorId)
                  .HasDatabaseName("IX_UserNotes_AuthorId")
                  .HasFilter("\"AuthorId\" IS NOT NULL");

            entity.HasIndex(un => un.IsArchived)
                  .HasDatabaseName("IX_UserNotes_IsArchived");
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

        // Handle Venue entities
        var venueEntries = ChangeTracker.Entries<Venue>();
        foreach (var entry in venueEntries)
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

        // Handle IncidentNote entities
        var incidentNoteEntries = ChangeTracker.Entries<IncidentNote>();
        foreach (var entry in incidentNoteEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
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

        // Handle CheckInSessionToken entities
        var sessionTokenEntries = ChangeTracker.Entries<CheckInSessionToken>();
        foreach (var entry in sessionTokenEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;

                // Ensure ExpiresAt is UTC
                if (entry.Entity.ExpiresAt.Kind != DateTimeKind.Utc)
                {
                    entry.Entity.ExpiresAt = DateTime.SpecifyKind(entry.Entity.ExpiresAt, DateTimeKind.Utc);
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
                // Only set PerformedAt if it hasn't been explicitly set (still at default)
                if (entry.Entity.PerformedAt == default)
                {
                    entry.Entity.PerformedAt = DateTime.UtcNow;
                }
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

        // Handle EventAttendance entities
        var eventAttendanceEntries = ChangeTracker.Entries<EventAttendance>();
        foreach (var entry in eventAttendanceEntries)
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

        // Handle AttendanceHistory entities
        var attendanceHistoryEntries = ChangeTracker.Entries<AttendanceHistory>();
        foreach (var entry in attendanceHistoryEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
        }

        // Handle ContentPage entities
        var contentPageEntries = ChangeTracker.Entries<ContentPage>();
        foreach (var entry in contentPageEntries)
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

        // Handle ContentRevision entities
        var contentRevisionEntries = ChangeTracker.Entries<ContentRevision>();
        foreach (var entry in contentRevisionEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
        }

        // Handle UserNote entities
        var userNoteEntries = ChangeTracker.Entries<WitchCityRope.Api.Data.Entities.UserNote>();
        foreach (var entry in userNoteEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
        }

        // Handle GlobalEmailTemplate entities
        var globalEmailTemplateEntries = ChangeTracker.Entries<GlobalEmailTemplate>();
        foreach (var entry in globalEmailTemplateEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
                // Increment version on update
                entry.Entity.Version++;
            }
        }

        // Handle EventEmailTemplate entities
        var eventEmailTemplateEntries = ChangeTracker.Entries<EventEmailTemplate>();
        foreach (var entry in eventEmailTemplateEntries)
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

        // Handle SentAdHocEmail entities
        var sentAdHocEmailEntries = ChangeTracker.Entries<SentAdHocEmail>();
        foreach (var entry in sentAdHocEmailEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.SentAt = DateTime.UtcNow;

                // Ensure ScheduledSendAt is UTC if set
                if (entry.Entity.ScheduledSendAt.HasValue && entry.Entity.ScheduledSendAt.Value.Kind != DateTimeKind.Utc)
                {
                    entry.Entity.ScheduledSendAt = DateTime.SpecifyKind(entry.Entity.ScheduledSendAt.Value, DateTimeKind.Utc);
                }
            }
        }

        // Handle AdHocEmailTemplate entities
        var adHocTemplateEntries = ChangeTracker.Entries<AdHocEmailTemplate>();
        foreach (var entry in adHocTemplateEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
        }

        // Handle EmailTriggerLog entities
        var triggerLogEntries = ChangeTracker.Entries<EmailTriggerLog>();
        foreach (var entry in triggerLogEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TriggeredAt = DateTime.UtcNow;

                if (entry.Entity.SentAt.HasValue && entry.Entity.SentAt.Value.Kind != DateTimeKind.Utc)
                {
                    entry.Entity.SentAt = DateTime.SpecifyKind(entry.Entity.SentAt.Value, DateTimeKind.Utc);
                }
            }
        }
    }
}
