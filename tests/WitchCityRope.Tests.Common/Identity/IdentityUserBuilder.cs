using System;
using Bogus;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Tests.Common.Builders;
using WitchCityRope.Tests.Common.Fixtures;
using WitchCityRope.Tests.Common.Helpers;

namespace WitchCityRope.Tests.Common.Identity
{
    /// <summary>
    /// Builder for creating WitchCityRopeUser instances for Identity-related testing
    /// Extends the core UserBuilder functionality with Identity-specific properties
    /// </summary>
    public class IdentityUserBuilder : TestDataBuilder<WitchCityRopeUser, IdentityUserBuilder>
    {
        private string _encryptedLegalName;
        private SceneName _sceneName;
        private EmailAddress _email;
        private DateTime _dateOfBirth;
        private UserRole _role = UserRole.Attendee;
        
        // Identity-specific properties
        private Guid _id = Guid.NewGuid();
        private bool _emailConfirmed = true;
        private string? _phoneNumber;
        private bool _phoneNumberConfirmed = false;
        private bool _twoFactorEnabled = false;
        private bool _lockoutEnabled = true;
        private DateTimeOffset? _lockoutEnd;
        private int _accessFailedCount = 0;
        private string _securityStamp = Guid.NewGuid().ToString();
        private string _concurrencyStamp = Guid.NewGuid().ToString();
        private bool _isVetted = false;
        private string? _pronouns;
        private string? _pronouncedName;
        private DateTime? _lastLoginAt;
        private int _failedLoginAttempts = 0;
        private DateTime? _lockedOutUntil;
        private DateTime? _lastPasswordChangeAt;

        public IdentityUserBuilder()
        {
            // Set default valid values
            _encryptedLegalName = _faker.Random.AlphaNumeric(32);
            _sceneName = SceneName.Create(_faker.Internet.UserName());
            _email = EmailAddress.Create(_faker.Internet.Email());
            _dateOfBirth = DateTimeFixture.ValidBirthDate;
        }

        #region Core Properties

        public IdentityUserBuilder WithId(Guid id)
        {
            _id = id;
            return This;
        }

        public IdentityUserBuilder WithEncryptedLegalName(string encryptedLegalName)
        {
            _encryptedLegalName = encryptedLegalName;
            return This;
        }

        public IdentityUserBuilder WithSceneName(string sceneName)
        {
            _sceneName = SceneName.Create(sceneName);
            return This;
        }

        public IdentityUserBuilder WithSceneName(SceneName sceneName)
        {
            _sceneName = sceneName;
            return This;
        }

        public IdentityUserBuilder WithEmail(string email)
        {
            _email = EmailAddress.Create(email);
            return This;
        }

        public IdentityUserBuilder WithEmail(EmailAddress email)
        {
            _email = email;
            return This;
        }

        public IdentityUserBuilder WithDateOfBirth(DateTime dateOfBirth)
        {
            _dateOfBirth = dateOfBirth;
            return This;
        }

        public IdentityUserBuilder WithAge(int age)
        {
            _dateOfBirth = DateTime.Today.AddYears(-age).AddDays(-1);
            return This;
        }

        public IdentityUserBuilder AsMinimumAge()
        {
            _dateOfBirth = DateTimeFixture.MinimumAgeBirthDate;
            return This;
        }

        public IdentityUserBuilder AsUnderAge()
        {
            _dateOfBirth = DateTimeFixture.UnderAgeBirthDate;
            return This;
        }

        public IdentityUserBuilder WithRole(UserRole role)
        {
            _role = role;
            return This;
        }

        public IdentityUserBuilder AsOrganizer()
        {
            _role = UserRole.Organizer;
            return This;
        }

        public IdentityUserBuilder AsAdministrator()
        {
            _role = UserRole.Administrator;
            return This;
        }

        public IdentityUserBuilder AsModerator()
        {
            _role = UserRole.Moderator;
            return This;
        }

        public IdentityUserBuilder AsMember()
        {
            _role = UserRole.Member;
            return This;
        }

        #endregion

        #region Identity-Specific Properties

        public IdentityUserBuilder WithEmailConfirmed(bool confirmed = true)
        {
            _emailConfirmed = confirmed;
            return This;
        }

        public IdentityUserBuilder WithUnconfirmedEmail()
        {
            _emailConfirmed = false;
            return This;
        }

        public IdentityUserBuilder WithPhoneNumber(string phoneNumber, bool confirmed = false)
        {
            _phoneNumber = phoneNumber;
            _phoneNumberConfirmed = confirmed;
            return This;
        }

        public IdentityUserBuilder WithConfirmedPhoneNumber(string phoneNumber)
        {
            _phoneNumber = phoneNumber;
            _phoneNumberConfirmed = true;
            return This;
        }

        public IdentityUserBuilder WithTwoFactorEnabled(bool enabled = true)
        {
            _twoFactorEnabled = enabled;
            return This;
        }

        public IdentityUserBuilder WithLockout(DateTimeOffset lockoutEnd, int failedAttempts = 5)
        {
            _lockoutEnd = lockoutEnd;
            _accessFailedCount = failedAttempts;
            return This;
        }

        public IdentityUserBuilder AsLockedOut(TimeSpan duration)
        {
            _lockoutEnd = DateTimeOffset.UtcNow.Add(duration);
            _accessFailedCount = 5;
            return This;
        }

        public IdentityUserBuilder WithFailedAccessAttempts(int count)
        {
            _accessFailedCount = count;
            return This;
        }

        public IdentityUserBuilder WithSecurityStamp(string stamp)
        {
            _securityStamp = stamp;
            return This;
        }

        public IdentityUserBuilder WithConcurrencyStamp(string stamp)
        {
            _concurrencyStamp = stamp;
            return This;
        }

        #endregion

        #region WitchCityRope-Specific Properties

        public IdentityUserBuilder AsVetted()
        {
            _isVetted = true;
            return This;
        }

        public IdentityUserBuilder WithPronouns(string pronouns)
        {
            _pronouns = pronouns;
            return This;
        }

        public IdentityUserBuilder WithPronouncedName(string pronouncedName)
        {
            _pronouncedName = pronouncedName;
            return This;
        }

        public IdentityUserBuilder WithLastLogin(DateTime lastLoginAt)
        {
            _lastLoginAt = lastLoginAt;
            return This;
        }

        public IdentityUserBuilder AsRecentlyLoggedIn()
        {
            _lastLoginAt = DateTime.UtcNow.AddMinutes(-5);
            return This;
        }

        public IdentityUserBuilder WithFailedLoginAttempts(int attempts)
        {
            _failedLoginAttempts = attempts;
            return This;
        }

        public IdentityUserBuilder WithPasswordChangedAt(DateTime changedAt)
        {
            _lastPasswordChangeAt = changedAt;
            return This;
        }

        #endregion

        #region Common Scenarios

        /// <summary>
        /// Creates a fully verified and active user
        /// </summary>
        public IdentityUserBuilder AsFullyVerified()
        {
            _emailConfirmed = true;
            _isVetted = true;
            _phoneNumberConfirmed = _phoneNumber != null;
            return This;
        }

        /// <summary>
        /// Creates a new unverified user
        /// </summary>
        public IdentityUserBuilder AsNewUser()
        {
            _emailConfirmed = false;
            _isVetted = false;
            _lastLoginAt = null;
            _role = UserRole.Attendee;
            return This;
        }

        /// <summary>
        /// Creates a user with security issues
        /// </summary>
        public IdentityUserBuilder AsCompromised()
        {
            _accessFailedCount = 4; // One more attempt before lockout
            _lastPasswordChangeAt = DateTime.UtcNow.AddMonths(-6); // Old password
            return This;
        }

        #endregion

        public override WitchCityRopeUser Build()
        {
            var user = new WitchCityRopeUser(
                _encryptedLegalName,
                _sceneName,
                _email,
                _dateOfBirth,
                _role);

            // Set the ID using reflection since it may have a private setter
            TestPropertySetter.SetProperty(user, nameof(user.Id), _id);

            // Set Identity properties
            user.EmailConfirmed = _emailConfirmed;
            user.PhoneNumber = _phoneNumber;
            user.PhoneNumberConfirmed = _phoneNumberConfirmed;
            user.TwoFactorEnabled = _twoFactorEnabled;
            user.LockoutEnabled = _lockoutEnabled;
            user.LockoutEnd = _lockoutEnd;
            user.AccessFailedCount = _accessFailedCount;
            user.SecurityStamp = _securityStamp;
            user.ConcurrencyStamp = _concurrencyStamp;

            // Set WitchCityRope-specific properties
            if (_isVetted)
            {
                user.MarkAsVetted();
            }

            if (_pronouns != null)
            {
                user.UpdatePronouns(_pronouns);
            }

            if (_pronouncedName != null)
            {
                user.UpdatePronouncedName(_pronouncedName);
            }

            user.LastLoginAt = _lastLoginAt;
            user.FailedLoginAttempts = _failedLoginAttempts;
            user.LockedOutUntil = _lockedOutUntil;
            user.LastPasswordChangeAt = _lastPasswordChangeAt;

            return user;
        }

        /// <summary>
        /// Creates multiple users with incremental differences
        /// </summary>
        public List<WitchCityRopeUser> BuildMany(int count, Action<IdentityUserBuilder, int>? customizer = null)
        {
            var users = new List<WitchCityRopeUser>();
            
            for (int i = 0; i < count; i++)
            {
                var builder = new IdentityUserBuilder()
                    .WithSceneName($"{_sceneName.Value}_{i}")
                    .WithEmail($"user{i}_{_email.Value}");

                customizer?.Invoke(builder, i);
                users.Add(builder.Build());
            }

            return users;
        }
    }

    /// <summary>
    /// Extension methods for IdentityUserBuilder
    /// </summary>
    public static class IdentityUserBuilderExtensions
    {
        /// <summary>
        /// Creates a builder with random valid data
        /// </summary>
        public static IdentityUserBuilder Random(this IdentityUserBuilder builder)
        {
            var faker = new Faker();
            return builder
                .WithSceneName(faker.Internet.UserName())
                .WithEmail(faker.Internet.Email())
                .WithAge(faker.Random.Int(21, 65))
                .WithPronouns(faker.PickRandom("he/him", "she/her", "they/them", "ze/zir"));
        }

        /// <summary>
        /// Creates an organizer with full permissions
        /// </summary>
        public static IdentityUserBuilder AsFullOrganizer(this IdentityUserBuilder builder)
        {
            return builder
                .AsOrganizer()
                .AsVetted()
                .WithEmailConfirmed()
                .AsRecentlyLoggedIn();
        }

        /// <summary>
        /// Creates an admin with full system access
        /// </summary>
        public static IdentityUserBuilder AsFullAdmin(this IdentityUserBuilder builder)
        {
            return builder
                .AsAdministrator()
                .AsVetted()
                .WithEmailConfirmed()
                .WithTwoFactorEnabled()
                .AsRecentlyLoggedIn();
        }
    }
}