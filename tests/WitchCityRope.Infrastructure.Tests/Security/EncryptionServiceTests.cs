using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using WitchCityRope.Infrastructure.Security;
using Xunit;

namespace WitchCityRope.Infrastructure.Tests.Security
{
    public class EncryptionServiceTests
    {
        private readonly EncryptionService _encryptionService;
        private readonly IConfiguration _configuration;

        public EncryptionServiceTests()
        {
            var configValues = new Dictionary<string, string?>
            {
                {"Encryption:Key", "TestEncryptionKey123456789012345678901234567890"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            _encryptionService = new EncryptionService(_configuration);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Configuration_Is_Null()
        {
            // Act & Assert
            var act = () => new EncryptionService(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_Should_Throw_When_Encryption_Key_Is_Missing()
        {
            // Arrange
            var emptyConfig = new ConfigurationBuilder().Build();

            // Act & Assert
            var act = () => new EncryptionService(emptyConfig);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Encryption key not configured");
        }

        [Fact]
        public async Task EncryptAsync_Should_Encrypt_Plain_Text()
        {
            // Arrange
            const string plainText = "This is a secret message";

            // Act
            var encrypted = await _encryptionService.EncryptAsync(plainText);

            // Assert
            encrypted.Should().NotBeNullOrEmpty();
            encrypted.Should().NotBe(plainText);
            encrypted.Should().MatchRegex("^[A-Za-z0-9+/=]+$"); // Base64 pattern
        }

        [Fact]
        public async Task EncryptAsync_Should_Return_Empty_String_For_Empty_Input()
        {
            // Act
            var encrypted = await _encryptionService.EncryptAsync(string.Empty);

            // Assert
            encrypted.Should().BeEmpty();
        }

        [Fact]
        public async Task EncryptAsync_Should_Return_Null_For_Null_Input()
        {
            // Act
            var encrypted = await _encryptionService.EncryptAsync(null!);

            // Assert
            encrypted.Should().BeNull();
        }

        [Fact]
        public async Task DecryptAsync_Should_Decrypt_Encrypted_Text()
        {
            // Arrange
            const string plainText = "This is a secret message";
            var encrypted = await _encryptionService.EncryptAsync(plainText);

            // Act
            var decrypted = await _encryptionService.DecryptAsync(encrypted);

            // Assert
            decrypted.Should().Be(plainText);
        }

        [Fact]
        public async Task DecryptAsync_Should_Return_Empty_String_For_Empty_Input()
        {
            // Act
            var decrypted = await _encryptionService.DecryptAsync(string.Empty);

            // Assert
            decrypted.Should().BeEmpty();
        }

        [Fact]
        public async Task DecryptAsync_Should_Return_Null_For_Null_Input()
        {
            // Act
            var decrypted = await _encryptionService.DecryptAsync(null!);

            // Assert
            decrypted.Should().BeNull();
        }

        [Fact]
        public async Task DecryptAsync_Should_Throw_For_Invalid_Base64()
        {
            // Arrange
            const string invalidBase64 = "This is not valid base64!@#$%";

            // Act & Assert
            var act = async () => await _encryptionService.DecryptAsync(invalidBase64);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Failed to decrypt data");
        }

        [Fact]
        public async Task DecryptAsync_Should_Throw_For_Corrupted_Data()
        {
            // Arrange
            const string corruptedData = "VGhpcyBpcyBub3QgZW5jcnlwdGVkIGRhdGE="; // Valid base64 but not encrypted data

            // Act & Assert
            var act = async () => await _encryptionService.DecryptAsync(corruptedData);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Failed to decrypt data");
        }

        [Fact]
        public async Task Encrypt_Decrypt_Should_Handle_Unicode_Characters()
        {
            // Arrange
            const string plainText = "Hello 世界 🌍 Émoji";

            // Act
            var encrypted = await _encryptionService.EncryptAsync(plainText);
            var decrypted = await _encryptionService.DecryptAsync(encrypted);

            // Assert
            decrypted.Should().Be(plainText);
        }

        [Fact]
        public async Task Encrypt_Should_Produce_Different_Results_For_Different_Inputs()
        {
            // Arrange
            const string plainText1 = "First secret";
            const string plainText2 = "Second secret";

            // Act
            var encrypted1 = await _encryptionService.EncryptAsync(plainText1);
            var encrypted2 = await _encryptionService.EncryptAsync(plainText2);

            // Assert
            encrypted1.Should().NotBe(encrypted2);
        }

        [Fact]
        public void Hash_Should_Create_BCrypt_Hash()
        {
            // Arrange
            const string password = "MySecurePassword123!";

            // Act
            var hash = _encryptionService.Hash(password);

            // Assert
            hash.Should().NotBeNullOrEmpty();
            hash.Should().NotBe(password);
            hash.Should().StartWith("$2"); // BCrypt hash prefix
        }

        [Fact]
        public void Hash_Should_Throw_For_Null_Input()
        {
            // Act & Assert
            var act = () => _encryptionService.Hash(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Hash_Should_Throw_For_Empty_Input()
        {
            // Act & Assert
            var act = () => _encryptionService.Hash(string.Empty);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Hash_Should_Produce_Different_Hashes_For_Same_Input()
        {
            // Arrange
            const string password = "MySecurePassword123!";

            // Act
            var hash1 = _encryptionService.Hash(password);
            var hash2 = _encryptionService.Hash(password);

            // Assert
            hash1.Should().NotBe(hash2); // Due to different salts
        }

        [Fact]
        public void VerifyHash_Should_Return_True_For_Correct_Password()
        {
            // Arrange
            const string password = "MySecurePassword123!";
            var hash = _encryptionService.Hash(password);

            // Act
            var result = _encryptionService.VerifyHash(password, hash);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void VerifyHash_Should_Return_False_For_Incorrect_Password()
        {
            // Arrange
            const string password = "MySecurePassword123!";
            const string wrongPassword = "WrongPassword123!";
            var hash = _encryptionService.Hash(password);

            // Act
            var result = _encryptionService.VerifyHash(wrongPassword, hash);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void VerifyHash_Should_Return_False_For_Null_PlainText()
        {
            // Arrange
            var hash = _encryptionService.Hash("password");

            // Act
            var result = _encryptionService.VerifyHash(null!, hash);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void VerifyHash_Should_Return_False_For_Null_Hash()
        {
            // Act
            var result = _encryptionService.VerifyHash("password", null!);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void VerifyHash_Should_Return_False_For_Invalid_Hash()
        {
            // Act
            var result = _encryptionService.VerifyHash("password", "not-a-valid-hash");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task Encryption_Should_Be_Deterministic_With_Same_Key()
        {
            // Arrange
            const string plainText = "Deterministic test";
            
            // Create two instances with the same key
            var service1 = new EncryptionService(_configuration);
            var service2 = new EncryptionService(_configuration);

            // Act
            var encrypted1 = await service1.EncryptAsync(plainText);
            var decrypted2 = await service2.DecryptAsync(encrypted1);

            // Assert
            decrypted2.Should().Be(plainText);
        }

        [Fact]
        public async Task Encryption_Should_Fail_With_Different_Keys()
        {
            // Arrange
            const string plainText = "Test with different keys";
            
            var config1 = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"Encryption:Key", "Key1234567890123456789012345678901234567890"}
                })
                .Build();
            
            var config2 = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"Encryption:Key", "DifferentKey1234567890123456789012345678901"}
                })
                .Build();

            var service1 = new EncryptionService(config1);
            var service2 = new EncryptionService(config2);

            // Act
            var encrypted = await service1.EncryptAsync(plainText);
            var act = async () => await service2.DecryptAsync(encrypted);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Failed to decrypt data");
        }
    }
}