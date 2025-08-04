using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WitchCityRope.Core.Entities;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Data;

namespace WitchCityRope.Infrastructure.Identity
{
    /// <summary>
    /// Helper class to migrate existing data to ASP.NET Core Identity
    /// </summary>
    public class IdentityMigrationHelper
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<IdentityMigrationHelper> _logger;

        public IdentityMigrationHelper(IServiceProvider serviceProvider, ILogger<IdentityMigrationHelper> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Migrates existing users from the old schema to Identity
        /// NOTE: This is commented out since we're starting fresh with Identity
        /// </summary>
        public async Task MigrateUsersAsync()
        {
            // Since this is a fresh project, we don't need migration from old schema
            _logger.LogInformation("Skipping user migration - starting fresh with Identity");
            await Task.CompletedTask;
            
            /* Original migration code kept for reference
            using var scope = _serviceProvider.CreateScope();
            var oldContext = scope.ServiceProvider.GetRequiredService<WitchCityRopeDbContext>();
            var newContext = scope.ServiceProvider.GetRequiredService<WitchCityRopeIdentityDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<WitchCityRopeUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<WitchCityRopeRole>>();

            try
            {
                // Migration code removed since we're starting fresh
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during user migration");
                throw;
            }
            */
        }

        /// <summary>
        /// Ensures all roles exist in the system
        /// </summary>
        public async Task EnsureRolesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<WitchCityRopeRole>>();
            
            var roles = Enum.GetValues<UserRole>();

            foreach (var role in roles)
            {
                var roleName = role.ToString();
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var identityRole = new WitchCityRopeRole(roleName)
                    {
                        Description = GetRoleDescription(role),
                        Priority = (int)role
                    };

                    var result = await roleManager.CreateAsync(identityRole);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Created role: {Role}", roleName);
                    }
                    else
                    {
                        _logger.LogError("Failed to create role {Role}: {Errors}", 
                            roleName, 
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
        }

        private string GetRoleDescription(UserRole role)
        {
            return role switch
            {
                UserRole.Attendee => "Standard event attendee",
                UserRole.Member => "Verified community member with additional privileges",
                UserRole.Organizer => "Event organizer who can create and manage events",
                UserRole.Moderator => "Community moderator who can review incidents and vetting",
                UserRole.Administrator => "System administrator with full access",
                _ => "Unknown role"
            };
        }

        /// <summary>
        /// Updates foreign key relationships after migration
        /// </summary>
        public async Task UpdateRelationshipsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WitchCityRopeIdentityDbContext>();

            try
            {
                // Since we're starting fresh, no relationships need updating
                _logger.LogInformation("No relationship updates needed - starting fresh");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating relationships");
                throw;
            }
        }
    }
}