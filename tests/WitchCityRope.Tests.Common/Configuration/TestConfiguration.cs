using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Tests.Common.Interfaces;

namespace WitchCityRope.Tests.Common.Configuration
{
    /// <summary>
    /// Provides test configuration setup for unit and integration tests
    /// </summary>
    public static class TestConfiguration
    {
        /// <summary>
        /// Creates a test configuration with default test values
        /// </summary>
        public static IConfiguration CreateTestConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // JWT Settings
                    ["JwtSettings:SecretKey"] = "TEST-SECRET-KEY-FOR-UNIT-TESTS-MINIMUM-32-CHARS",
                    ["JwtSettings:Issuer"] = "WitchCityRope-Tests",
                    ["JwtSettings:Audience"] = "WitchCityRopeUsers-Tests",
                    ["JwtSettings:ExpirationMinutes"] = "60",
                    ["JwtSettings:RefreshTokenExpirationDays"] = "7",

                    // Email Settings
                    ["Email:From"] = "test@witchcityrope.com",
                    ["Email:FromName"] = "WitchCity Rope Tests",
                    ["Email:SendGrid:ApiKey"] = "SG.test-api-key",
                    ["Email:SendGrid:SandboxMode"] = "true",

                    // PayPal Settings
                    ["PayPal:ClientId"] = "test-paypal-client",
                    ["PayPal:ClientSecret"] = "test-paypal-secret",
                    ["PayPal:Mode"] = "sandbox",
                    ["PayPal:WebhookId"] = "test-webhook-id",

                    // Stripe Settings
                    ["Stripe:PublishableKey"] = "pk_test_123456789",
                    ["Stripe:SecretKey"] = "sk_test_123456789",
                    ["Stripe:WebhookSecret"] = "whsec_test_secret",

                    // Security Settings
                    ["Security:EncryptionKey"] = "TEST-ENCRYPTION-KEY-32-CHARACTERS",
                    ["Security:PasswordMinLength"] = "8",
                    ["Security:PasswordRequireDigit"] = "true",
                    ["Security:PasswordRequireUppercase"] = "true",
                    ["Security:PasswordRequireNonAlphanumeric"] = "true",

                    // Features
                    ["Features:EnableRegistration"] = "true",
                    ["Features:RequireEmailVerification"] = "false",
                    ["Features:EnableTwoFactor"] = "false",
                    ["Features:EnablePayments"] = "false",
                    ["Features:EnableVetting"] = "true",

                    // Database
                    ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
                    ["UseRedis"] = "false"
                })
                .Build();

            return configuration;
        }

        /// <summary>
        /// Adds test services with mocked external dependencies
        /// </summary>
        public static IServiceCollection AddTestServices(this IServiceCollection services)
        {
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Add mocked services
            services.AddSingleton(Mock.Of<IJwtService>());
            services.AddSingleton(Mock.Of<IEmailService>());
            services.AddSingleton(Mock.Of<IEncryptionService>());
            services.AddSingleton(Mock.Of<IUserRepository>());
            services.AddSingleton(Mock.Of<IUserContext>());
            services.AddSingleton(Mock.Of<ISlugGenerator>());

            return services;
        }

        /// <summary>
        /// Creates a service provider with test configuration
        /// </summary>
        public static ServiceProvider CreateTestServiceProvider()
        {
            var services = new ServiceCollection();
            var configuration = CreateTestConfiguration();

            services.AddSingleton<IConfiguration>(configuration);
            services.AddTestServices();

            return services.BuildServiceProvider();
        }
    }

    /// <summary>
    /// Test configuration for external services
    /// </summary>
    public class ExternalServiceTestConfig
    {
        public SendGridTestConfig SendGrid { get; set; } = new();
        public PayPalTestConfig PayPal { get; set; } = new();
        public StripeTestConfig Stripe { get; set; } = new();
    }

    /// <summary>
    /// SendGrid test configuration
    /// </summary>
    public class SendGridTestConfig
    {
        public bool UseSandboxMode { get; set; } = true;
        public string TestApiKey { get; set; } = "SG.test-api-key";
        public Dictionary<string, string> TestTemplates { get; set; } = new()
        {
            ["Welcome"] = "d-test-welcome",
            ["EmailVerification"] = "d-test-verification",
            ["PasswordReset"] = "d-test-reset"
        };
    }

    /// <summary>
    /// PayPal test configuration
    /// </summary>
    public class PayPalTestConfig
    {
        public string TestClientId { get; set; } = "test-paypal-client";
        public string TestClientSecret { get; set; } = "test-paypal-secret";
        public string Mode { get; set; } = "sandbox";
        public bool UseWebhooks { get; set; } = false;
    }

    /// <summary>
    /// Stripe test configuration
    /// </summary>
    public class StripeTestConfig
    {
        public string TestPublishableKey { get; set; } = "pk_test_123456789";
        public string TestSecretKey { get; set; } = "sk_test_123456789";
        public bool UseWebhooks { get; set; } = false;
        public string? WebhookEndpointSecret { get; set; }
    }
}