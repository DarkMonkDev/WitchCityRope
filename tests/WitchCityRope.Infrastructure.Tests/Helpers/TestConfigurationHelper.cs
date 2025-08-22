using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace WitchCityRope.Infrastructure.Tests.Helpers
{
    /// <summary>
    /// Helper class for creating test configurations
    /// </summary>
    public static class TestConfigurationHelper
    {
        public static IConfiguration CreateConfiguration(Dictionary<string, string?>? values = null)
        {
            var builder = new ConfigurationBuilder();
            
            if (values != null)
            {
                builder.AddInMemoryCollection(values);
            }
            
            return builder.Build();
        }

        public static IConfiguration CreateDefaultConfiguration()
        {
            var values = new Dictionary<string, string?>
            {
                // Encryption settings
                {"Encryption:Key", "TestEncryptionKey123456789012345678901234567890"},
                
                // JWT settings
                {"Jwt:SecretKey", "TestJwtSecretKey123456789012345678901234567890"},
                {"Jwt:Issuer", "WitchCityRope.Tests"},
                {"Jwt:Audience", "WitchCityRope.Tests"},
                {"Jwt:ExpirationMinutes", "60"},
                
                // SendGrid settings
                {"SendGrid:ApiKey", "test-api-key"},
                {"SendGrid:FromEmail", "test@witchcityrope.com"},
                {"SendGrid:FromName", "WCR Tests"},
                {"SendGrid:Templates:RegistrationConfirmation", "test-reg-template"},
                {"SendGrid:Templates:CancellationConfirmation", "test-cancel-template"},
                {"SendGrid:Templates:VettingStatusUpdate", "test-vetting-template"},
                
                // PayPal settings
                {"PayPal:ClientId", "test-client-id"},
                {"PayPal:ClientSecret", "test-client-secret"},
                {"PayPal:IsSandbox", "true"},
                
                // Database settings
                {"ConnectionStrings:DefaultConnection", "Server=localhost;Database=WitchCityRope_Test;Trusted_Connection=true;"}
            };

            return CreateConfiguration(values);
        }

        public static IConfiguration CreateEncryptionConfiguration(string? encryptionKey = null)
        {
            var values = new Dictionary<string, string?>();
            
            if (!string.IsNullOrEmpty(encryptionKey))
            {
                values["Encryption:Key"] = encryptionKey;
            }
            
            return CreateConfiguration(values);
        }

        public static IConfiguration CreateJwtConfiguration(
            string? secretKey = null,
            string? issuer = null,
            string? audience = null,
            int? expirationMinutes = null)
        {
            var values = new Dictionary<string, string?>();
            
            if (!string.IsNullOrEmpty(secretKey))
                values["Jwt:SecretKey"] = secretKey;
            
            if (!string.IsNullOrEmpty(issuer))
                values["Jwt:Issuer"] = issuer;
            
            if (!string.IsNullOrEmpty(audience))
                values["Jwt:Audience"] = audience;
            
            if (expirationMinutes.HasValue)
                values["Jwt:ExpirationMinutes"] = expirationMinutes.Value.ToString();
            
            return CreateConfiguration(values);
        }

        public static IConfiguration CreateSendGridConfiguration(
            string? apiKey = null,
            string? fromEmail = null,
            string? fromName = null)
        {
            var values = new Dictionary<string, string?>();
            
            if (!string.IsNullOrEmpty(apiKey))
                values["SendGrid:ApiKey"] = apiKey;
            
            if (!string.IsNullOrEmpty(fromEmail))
                values["SendGrid:FromEmail"] = fromEmail;
            
            if (!string.IsNullOrEmpty(fromName))
                values["SendGrid:FromName"] = fromName;
            
            return CreateConfiguration(values);
        }

        public static IConfiguration CreatePayPalConfiguration(
            string? clientId = null,
            string? clientSecret = null,
            bool? isSandbox = null)
        {
            var values = new Dictionary<string, string?>();
            
            if (!string.IsNullOrEmpty(clientId))
                values["PayPal:ClientId"] = clientId;
            
            if (!string.IsNullOrEmpty(clientSecret))
                values["PayPal:ClientSecret"] = clientSecret;
            
            if (isSandbox.HasValue)
                values["PayPal:IsSandbox"] = isSandbox.Value.ToString();
            
            return CreateConfiguration(values);
        }
    }
}