using System;
using Microsoft.AspNetCore.Identity;

namespace WitchCityRope.Infrastructure.Identity
{
    /// <summary>
    /// Custom role entity that extends IdentityRole with WitchCityRope-specific properties
    /// </summary>
    public class WitchCityRopeRole : IdentityRole<Guid>
    {
        // Required for EF Core
        protected WitchCityRopeRole() { }

        public WitchCityRopeRole(string roleName) : base(roleName)
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            IsActive = true;
        }

        /// <summary>
        /// Description of the role's purpose and permissions
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Indicates if the role is currently active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Priority level for role hierarchy (higher number = higher priority)
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Updates the role description
        /// </summary>
        public void UpdateDescription(string description)
        {
            Description = description;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Deactivates the role
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Reactivates the role
        /// </summary>
        public void Reactivate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}