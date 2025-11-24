using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Tests.Common.Fixtures;

namespace WitchCityRope.Tests.Common.Builders
{
    public class UserBuilder : TestDataBuilder<User, UserBuilder>
    {
        private string _encryptedLegalName;
        private SceneName _sceneName;
        private EmailAddress _email;
        private DateTime _dateOfBirth;
        private UserRole _role = UserRole.Attendee;

        public UserBuilder()
        {
            // Set default valid values
            _encryptedLegalName = _faker.Random.AlphaNumeric(32);
            _sceneName = SceneName.Create(_faker.Internet.UserName());
            _email = EmailAddress.Create(_faker.Internet.Email());
            _dateOfBirth = DateTimeFixture.ValidBirthDate;
        }

        public UserBuilder WithEncryptedLegalName(string encryptedLegalName)
        {
            _encryptedLegalName = encryptedLegalName;
            return This;
        }

        public UserBuilder WithSceneName(string sceneName)
        {
            _sceneName = SceneName.Create(sceneName);
            return This;
        }

        public UserBuilder WithSceneName(SceneName sceneName)
        {
            _sceneName = sceneName;
            return This;
        }

        public UserBuilder WithEmail(string email)
        {
            _email = EmailAddress.Create(email);
            return This;
        }

        public UserBuilder WithEmail(EmailAddress email)
        {
            _email = email;
            return This;
        }

        public UserBuilder WithDateOfBirth(DateTime dateOfBirth)
        {
            _dateOfBirth = dateOfBirth;
            return This;
        }

        public UserBuilder WithAge(int age)
        {
            _dateOfBirth = DateTime.Today.AddYears(-age).AddDays(-1);
            return This;
        }

        public UserBuilder AsMinimumAge()
        {
            _dateOfBirth = DateTimeFixture.MinimumAgeBirthDate;
            return This;
        }

        public UserBuilder AsUnderAge()
        {
            _dateOfBirth = DateTimeFixture.UnderAgeBirthDate;
            return This;
        }

        public UserBuilder WithRole(UserRole role)
        {
            _role = role;
            return This;
        }

        public UserBuilder AsOrganizer()
        {
            _role = UserRole.Organizer;
            return This;
        }

        public UserBuilder AsAdministrator()
        {
            _role = UserRole.Administrator;
            return This;
        }

        public UserBuilder AsModerator()
        {
            _role = UserRole.Moderator;
            return This;
        }

        public UserBuilder AsMember()
        {
            _role = UserRole.Member;
            return This;
        }

        public override User Build()
        {
            return new User(
                _encryptedLegalName,
                _sceneName,
                _email,
                _dateOfBirth,
                _role
            );
        }
    }
}