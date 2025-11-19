using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VettedMemberImport.Entities;

namespace VettedMemberImport.Data;

/// <summary>
/// Database context for vetted member import tool
/// Minimal DbContext with only the tables needed for import
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<VettingApplication> VettingApplications { get; set; } = null!;
    public DbSet<VettingAuditLog> VettingAuditLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure table names to match API database schema
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<VettingApplication>().ToTable("VettingApplications");
        builder.Entity<VettingAuditLog>().ToTable("VettingAuditLogs");
    }
}
