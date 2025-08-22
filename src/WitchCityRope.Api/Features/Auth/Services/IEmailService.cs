using System.Threading.Tasks;

namespace WitchCityRope.Api.Features.Auth.Services
{
    /// <summary>
    /// Interface for email operations in the Auth feature
    /// </summary>
    public interface IEmailService
    {
        Task<bool> SendVerificationEmailAsync(string email, string displayName, string token);
    }

    /// <summary>
    /// Email message model for Auth feature
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
}