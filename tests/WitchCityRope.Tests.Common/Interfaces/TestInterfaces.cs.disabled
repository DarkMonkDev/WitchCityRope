using System;
using System.Threading.Tasks;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.ValueObjects;

namespace WitchCityRope.Tests.Common.Interfaces
{
    /// <summary>
    /// JWT service interface for test purposes
    /// </summary>
    public interface IJwtService
    {
        Task<JwtToken> GenerateTokenAsync(User user);
        Task<bool> ValidateTokenAsync(string token);
        Task<Guid?> GetUserIdFromTokenAsync(string token);
        Task<string> GenerateRefreshTokenAsync();
    }

    /// <summary>
    /// JWT token model for tests
    /// </summary>
    public class JwtToken
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Email service interface for test purposes
    /// </summary>
    public interface IEmailService
    {
        Task<bool> SendAsync(EmailMessage message);
        Task<bool> SendTemplateAsync(string to, string templateId, object templateData);
        Task<bool> SendWelcomeEmailAsync(string to, string name);
        Task<bool> SendPasswordResetEmailAsync(string to, string resetToken);
        Task<bool> SendEmailVerificationAsync(string to, string verificationToken);
    }

    /// <summary>
    /// Email message model for tests
    /// </summary>
    public class EmailMessage
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; }
        public string? From { get; set; }
        public string? FromName { get; set; }
    }

    /// <summary>
    /// Encryption service interface for test purposes
    /// </summary>
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
        byte[] Encrypt(byte[] data);
        byte[] Decrypt(byte[] encryptedData);
    }

    /// <summary>
    /// User repository interface for test purposes
    /// </summary>
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetBySceneNameAsync(string sceneName);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> SceneNameExistsAsync(string sceneName);
        Task<User> CreateAsync(User user);
        Task<User> UpdateAsync(User user);
        Task DeleteAsync(Guid id);
        Task<List<User>> GetAllAsync();
        Task<RefreshToken?> GetRefreshTokenAsync(string token);
        Task SaveRefreshTokenAsync(RefreshToken refreshToken);
        Task DeleteRefreshTokenAsync(string token);
        Task DeleteExpiredRefreshTokensAsync();
    }

    /// <summary>
    /// Refresh token model for tests
    /// </summary>
    public class RefreshToken
    {
        public string Token { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? DeviceInfo { get; set; }
    }

    /// <summary>
    /// User context interface for test purposes
    /// </summary>
    public interface IUserContext
    {
        Task<User?> GetCurrentUserAsync();
        Guid? GetCurrentUserId();
        bool IsAuthenticated();
        bool IsInRole(string role);
        Task<bool> HasPermissionAsync(string permission);
    }

    /// <summary>
    /// Slug generator interface for test purposes
    /// </summary>
    public interface ISlugGenerator
    {
        string GenerateSlug(string text);
        string GenerateUniqueSlug(string text, Func<string, Task<bool>> existsCheck);
    }

    /// <summary>
    /// Payment request model for tests
    /// </summary>
    public class PaymentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Description { get; set; } = string.Empty;
        public string? PaymentMethodId { get; set; }
        public string? CustomerId { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Payment result model for tests
    /// </summary>
    public class PaymentResult
    {
        public bool Success { get; set; }
        public decimal AmountCharged { get; set; }
        public string? TransactionId { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }

    /// <summary>
    /// Vetting reference model for tests
    /// </summary>
    public class VettingReference
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }

    /// <summary>
    /// Payment service interface for test purposes
    /// </summary>
    public interface IPaymentService
    {
        Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
        Task<RefundResult> RefundPaymentAsync(string transactionId, decimal amount, string reason);
        Task<PaymentStatus> GetPaymentStatusAsync(string transactionId);
    }

    /// <summary>
    /// Refund result model for tests
    /// </summary>
    public class RefundResult
    {
        public bool Success { get; set; }
        public string? RefundId { get; set; }
        public decimal RefundedAmount { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Payment status enum for tests
    /// </summary>
    public enum PaymentStatus
    {
        NotFound,
        Completed,
        PartiallyRefunded,
        Refunded,
        Failed
    }
}