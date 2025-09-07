using System;
using System.Collections.Generic;
using System.Linq;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Represents a user in the WitchCityRope system
    /// Business Rules:
    /// - Users must be at least 21 years old
    /// - Legal names are encrypted for privacy
    /// - Scene names are public-facing identifiers
    /// </summary>
    public class User
    {
        private readonly List<Registration> _registrations = new();
        private readonly List<VettingApplication> _vettingApplications = new();

        // Private constructor for EF Core
        private User() { }

        public User(
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
            Email = email ?? throw new ArgumentNullException(nameof(email));
            DateOfBirth = dateOfBirth;
            Role = role;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            IsActive = true;
            IsVetted = false;
            PronouncedName = sceneName.Value;
            Pronouns = string.Empty;
        }

        public Guid Id { get; private set; }
        
        /// <summary>
        /// Legal name stored encrypted for privacy compliance
        /// </summary>
        public string EncryptedLegalName { get; private set; }
        
        /// <summary>
        /// Public-facing name used within the community
        /// </summary>
        public SceneName SceneName { get; private set; }
        
        public EmailAddress Email { get; private set; }
        
        public DateTime DateOfBirth { get; private set; }
        
        public UserRole Role { get; private set; }
        
        public bool IsActive { get; private set; }
        
        public DateTime CreatedAt { get; private set; }
        
        public DateTime UpdatedAt { get; private set; }
        
        /// <summary>
        /// Display name derived from scene name
        /// </summary>
        public string DisplayName => SceneName?.Value ?? "Unknown";
        
        /// <summary>
        /// First name for compatibility (derived from scene name)
        /// </summary>
        public string? FirstName => SceneName?.Value?.Split(' ').FirstOrDefault();
        
        /// <summary>
        /// Last name for compatibility (derived from scene name)
        /// </summary>
        public string? LastName => SceneName?.Value?.Contains(' ') == true 
            ? string.Join(" ", SceneName.Value.Split(' ').Skip(1))
            : null;
        
        /// <summary>
        /// Phonetic pronunciation of the user's name
        /// </summary>
        public string PronouncedName { get; private set; }
        
        /// <summary>
        /// User's preferred pronouns
        /// </summary>
        public string Pronouns { get; private set; }
        
        /// <summary>
        /// Indicates if the user has been vetted
        /// </summary>
        public bool IsVetted { get; private set; }
        
        public IReadOnlyCollection<Registration> Registrations => _registrations.AsReadOnly();
        
        public IReadOnlyCollection<VettingApplication> VettingApplications => _vettingApplications.AsReadOnly();

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
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the user's email address
        /// </summary>
        public void UpdateEmail(EmailAddress newEmail)
        {
            Email = newEmail ?? throw new ArgumentNullException(nameof(newEmail));
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
    }
}