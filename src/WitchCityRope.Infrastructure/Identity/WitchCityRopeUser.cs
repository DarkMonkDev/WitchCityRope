using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using WitchCityRope.Core;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;

namespace WitchCityRope.Infrastructure.Identity
{
    /// <summary>
    /// Custom user entity that extends IdentityUser with WitchCityRope-specific properties
    /// </summary>
    public class WitchCityRopeUser : IdentityUser<Guid>, IUser
    {
        private readonly List<Ticket> _tickets = new();
        private readonly List<VettingApplication> _vettingApplications = new();

        // Required for EF Core
        public WitchCityRopeUser() 
        {
            EmailVerificationToken = string.Empty;
            PronouncedName = string.Empty;
            Pronouns = string.Empty;
            EncryptedLegalName = string.Empty;
            SceneNameValue = string.Empty;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public WitchCityRopeUser(
            string encryptedLegalName,
            SceneName sceneName,
            EmailAddress email,
            DateTime dateOfBirth,
            UserRole role = UserRole.Attendee)
        {
            ValidateAge(dateOfBirth);

            Id = Guid.NewGuid();
            EncryptedLegalName = encryptedLegalName ?? throw new ArgumentNullException(nameof(encryptedLegalName));
            SceneName = sceneName ?? throw new ArgumentNullException(nameof(sceneName));
            EmailAddress = email ?? throw new ArgumentNullException(nameof(email));
            SceneNameValue = sceneName.Value;
            Email = email.Value; // Set the base IdentityUser Email
            NormalizedEmail = email.Value.ToUpperInvariant();
            UserName = email.Value; // Default username to email, can be overridden
            NormalizedUserName = email.Value.ToUpperInvariant();
            DateOfBirth = dateOfBirth;
            Role = role;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            IsActive = true;
            IsVetted = false;
            VettingStatus = VettingStatus.NotStarted;
            PronouncedName = sceneName.Value;
            Pronouns = string.Empty;
            EmailVerificationToken = string.Empty; // Initialize to empty string to prevent null constraint violation
            EmailVerificationTokenCreatedAt = null;
            FailedLoginAttempts = 0;
            LastLoginAt = null;
            LockedOutUntil = null;
            LastPasswordChangeAt = null;
            
            // Initialize IdentityUser properties
            EmailConfirmed = false;
            PhoneNumber = null;
            PhoneNumberConfirmed = false;
            TwoFactorEnabled = false;
            LockoutEnd = null;
            LockoutEnabled = true; // Typically enabled by default
            AccessFailedCount = 0;
            SecurityStamp = Guid.NewGuid().ToString();
            ConcurrencyStamp = Guid.NewGuid().ToString();
        }

        #region Custom Properties

        /// <summary>
        /// Legal name stored encrypted for privacy compliance
        /// </summary>
        public string EncryptedLegalName { get; private set; }

        /// <summary>
        /// Public-facing name used within the community
        /// </summary>
        public SceneName SceneName 
        {
            get => _sceneName ?? (!string.IsNullOrEmpty(SceneNameValue) ? SceneName.Create(SceneNameValue) : null);
            private set => _sceneName = value;
        }
        private SceneName _sceneName;
        
        /// <summary>
        /// Scene name as string for EF Core
        /// </summary>
        public string SceneNameValue { get; private set; }

        /// <summary>
        /// Strongly-typed email address (in addition to base Email property)
        /// </summary>
        public EmailAddress EmailAddress 
        {
            get => _emailAddress ?? (!string.IsNullOrEmpty(Email) ? EmailAddress.Create(Email) : null);
            private set => _emailAddress = value;
        }
        private EmailAddress _emailAddress;

        public DateTime DateOfBirth { get; private set; }

        public UserRole Role { get; private set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Display name derived from scene name
        /// </summary>
        public string DisplayName => SceneName?.Value ?? "Unknown";

        /// <summary>
        /// Phonetic pronunciation of the user's name
        /// </summary>
        public string PronouncedName { get; set; }

        /// <summary>
        /// User's preferred pronouns
        /// </summary>
        public string Pronouns { get; set; }

        /// <summary>
        /// Indicates if the user has been vetted
        /// </summary>
        public bool IsVetted { get; set; }

        /// <summary>
        /// Vetting status for the user
        /// </summary>
        public VettingStatus VettingStatus { get; set; } = VettingStatus.NotStarted;

        /// <summary>
        /// Extended user information (self-reference for compatibility)
        /// </summary>
        public WitchCityRopeUser UserExtended => this;

        /// <summary>
        /// Last login timestamp
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Failed login attempts (for lockout tracking)
        /// </summary>
        public int FailedLoginAttempts { get; set; }

        /// <summary>
        /// Lockout end date
        /// </summary>
        public DateTime? LockedOutUntil { get; set; }

        /// <summary>
        /// Last password change timestamp
        /// </summary>
        public DateTime? LastPasswordChangeAt { get; set; }

        /// <summary>
        /// Email verification token
        /// </summary>
        public string EmailVerificationToken { get; set; }

        /// <summary>
        /// Email verification token creation time
        /// </summary>
        public DateTime? EmailVerificationTokenCreatedAt { get; set; }

        #endregion

        #region Navigation Properties

        public IReadOnlyCollection<Ticket> Tickets => _tickets.AsReadOnly();

        public IReadOnlyCollection<VettingApplication> VettingApplications => _vettingApplications.AsReadOnly();

        // Navigation property for refresh tokens
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        // Navigation property for registrations
        public ICollection<Registration> Registrations { get; set; } = new List<Registration>();

        #endregion

        #region Methods

        /// <summary>
        /// Calculates user's age
        /// </summary>
        public int GetAge()
        {
            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Year;
            if (DateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }

        /// <summary>
        /// Updates the user's scene name
        /// </summary>
        public void UpdateSceneName(SceneName newSceneName)
        {
            SceneName = newSceneName ?? throw new ArgumentNullException(nameof(newSceneName));
            SceneNameValue = newSceneName.Value;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the user's email address
        /// </summary>
        public void UpdateEmail(EmailAddress newEmail)
        {
            EmailAddress = newEmail ?? throw new ArgumentNullException(nameof(newEmail));
            Email = newEmail.Value;
            NormalizedEmail = newEmail.Value.ToUpperInvariant();
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Promotes user to a new role
        /// </summary>
        public void PromoteToRole(UserRole newRole)
        {
            if (newRole <= Role)
                throw new DomainException("Cannot demote user or assign same role");

            Role = newRole;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Deactivates the user account
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Reactivates the user account
        /// </summary>
        public void Reactivate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the user's pronounced name
        /// </summary>
        public void UpdatePronouncedName(string pronouncedName)
        {
            PronouncedName = pronouncedName ?? throw new ArgumentNullException(nameof(pronouncedName));
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the user's pronouns
        /// </summary>
        public void UpdatePronouns(string pronouns)
        {
            Pronouns = pronouns ?? throw new ArgumentNullException(nameof(pronouns));
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the user as vetted
        /// </summary>
        public void MarkAsVetted()
        {
            IsVetted = true;
            VettingStatus = VettingStatus.Approved;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Sets the username to support login by scene name
        /// </summary>
        public void SetSceneNameAsUsername()
        {
            UserName = SceneNameValue;
            NormalizedUserName = SceneNameValue.ToUpperInvariant();
            UpdatedAt = DateTime.UtcNow;
        }

        private void ValidateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age)) age--;

            if (age < 21)
                throw new DomainException("User must be at least 21 years old");
        }

        #endregion
    }
}