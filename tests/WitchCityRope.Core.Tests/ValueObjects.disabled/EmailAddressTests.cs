using FluentAssertions;
using WitchCityRope.Core;
using WitchCityRope.Core.ValueObjects;
using Xunit;

namespace WitchCityRope.Core.Tests.ValueObjects
{
    [Trait("Category", "Unit")]
    public class EmailAddressTests
    {
        [Theory]
        [InlineData("test@example.com")]
        [InlineData("user.name@example.com")]
        [InlineData("user+tag@example.co.uk")]
        [InlineData("test123@subdomain.example.com")]
        [InlineData("TEST@EXAMPLE.COM")]
        public void Create_ValidEmail_CreatesSuccessfully(string email)
        {
            // Act
            var emailAddress = EmailAddress.Create(email);

            // Assert
            emailAddress.Should().NotBeNull();
            emailAddress.Value.Should().Be(email.ToLowerInvariant());
            emailAddress.DisplayValue.Should().Be(email.Trim());
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Create_EmptyOrNull_ThrowsArgumentException(string email)
        {
            // Act
            var action = () => EmailAddress.Create(email);

            // Assert
            action.Should().Throw<ArgumentException>()
                .WithMessage("Email address cannot be empty*");
        }

        [Theory]
        [InlineData("notanemail")]
        [InlineData("@example.com")]
        [InlineData("user@")]
        [InlineData("user@@example.com")]
        [InlineData("user@.com")]
        [InlineData("user name@example.com")]
        [InlineData("user@exam ple.com")]
        public void Create_InvalidFormat_ThrowsDomainException(string email)
        {
            // Act
            var action = () => EmailAddress.Create(email);

            // Assert
            action.Should().Throw<DomainException>()
                .WithMessage($"*Invalid email address format: {email}*");
        }

        [Fact]
        public void Create_EmailExceedsMaxLength_ThrowsDomainException()
        {
            // Arrange
            var longEmail = new string('a', 250) + "@example.com";

            // Act
            var action = () => EmailAddress.Create(longEmail);

            // Assert
            action.Should().Throw<DomainException>();
        }

        [Fact]
        public void Create_EmailWithWhitespace_TrimsSuccessfully()
        {
            // Arrange
            var emailWithSpaces = "  test@example.com  ";

            // Act
            var emailAddress = EmailAddress.Create(emailWithSpaces);

            // Assert
            emailAddress.Value.Should().Be("test@example.com");
            emailAddress.DisplayValue.Should().Be("test@example.com");
        }

        [Fact]
        public void GetDomain_ValidEmail_ReturnsDomain()
        {
            // Arrange
            var emailAddress = EmailAddress.Create("test@example.com");

            // Act
            var domain = emailAddress.GetDomain();

            // Assert
            domain.Should().Be("example.com");
        }

        [Fact]
        public void GetLocalPart_ValidEmail_ReturnsLocalPart()
        {
            // Arrange
            var emailAddress = EmailAddress.Create("test.user@example.com");

            // Act
            var localPart = emailAddress.GetLocalPart();

            // Assert
            localPart.Should().Be("test.user");
        }

        [Fact]
        public void Equals_SameEmailDifferentCase_ReturnsTrue()
        {
            // Arrange
            var email1 = EmailAddress.Create("Test@Example.com");
            var email2 = EmailAddress.Create("test@example.com");

            // Act & Assert
            email1.Should().Be(email2);
            (email1 == email2).Should().BeTrue();
            (email1 != email2).Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentEmails_ReturnsFalse()
        {
            // Arrange
            var email1 = EmailAddress.Create("test1@example.com");
            var email2 = EmailAddress.Create("test2@example.com");

            // Act & Assert
            email1.Should().NotBe(email2);
            (email1 == email2).Should().BeFalse();
            (email1 != email2).Should().BeTrue();
        }

        [Fact]
        public void Equals_NullEmail_ReturnsFalse()
        {
            // Arrange
            var email = EmailAddress.Create("test@example.com");

            // Act & Assert
            email.Equals(null).Should().BeFalse();
            (email == null).Should().BeFalse();
            (null == email).Should().BeFalse();
        }

        [Fact]
        public void GetHashCode_SameEmailDifferentCase_ReturnsSameHash()
        {
            // Arrange
            var email1 = EmailAddress.Create("Test@Example.com");
            var email2 = EmailAddress.Create("test@example.com");

            // Act & Assert
            email1.GetHashCode().Should().Be(email2.GetHashCode());
        }

        [Fact]
        public void ToString_ReturnsDisplayValue()
        {
            // Arrange
            var email = EmailAddress.Create("Test@Example.com");

            // Act
            var result = email.ToString();

            // Assert
            result.Should().Be("Test@Example.com");
        }

        [Fact]
        public void ImplicitStringConversion_ReturnsDisplayValue()
        {
            // Arrange
            var email = EmailAddress.Create("Test@Example.com");

            // Act
            string result = email;

            // Assert
            result.Should().Be("Test@Example.com");
        }

        [Fact]
        public void TryCreate_ValidEmail_ReturnsTrue()
        {
            // Act
            var result = EmailAddress.TryCreate("test@example.com", out var email);

            // Assert
            result.Should().BeTrue();
            email.Should().NotBeNull();
            email.Value.Should().Be("test@example.com");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("notanemail")]
        [InlineData("@example.com")]
        public void TryCreate_InvalidEmail_ReturnsFalse(string invalidEmail)
        {
            // Act
            var result = EmailAddress.TryCreate(invalidEmail, out var email);

            // Assert
            result.Should().BeFalse();
            email.Should().BeNull();
        }

        [Theory]
        [InlineData("test@example.com", "test@example.com")]
        [InlineData("TEST@EXAMPLE.COM", "test@example.com")]
        [InlineData("Test@Example.Com", "test@example.com")]
        public void Value_AlwaysLowercase(string input, string expected)
        {
            // Act
            var email = EmailAddress.Create(input);

            // Assert
            email.Value.Should().Be(expected);
        }

        [Theory]
        [InlineData("test@example.com", "test@example.com")]
        [InlineData("TEST@EXAMPLE.COM", "TEST@EXAMPLE.COM")]
        [InlineData("Test@Example.Com", "Test@Example.Com")]
        public void DisplayValue_PreservesOriginalCase(string input, string expected)
        {
            // Act
            var email = EmailAddress.Create(input);

            // Assert
            email.DisplayValue.Should().Be(expected);
        }
    }
}