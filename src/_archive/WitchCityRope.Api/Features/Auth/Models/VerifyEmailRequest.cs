using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Auth.Models
{
    /// <summary>
    /// Request model for email verification
    /// </summary>
    public class VerifyEmailRequest
    {
        [Required(ErrorMessage = "Verification token is required")]
        public string Token { get; set; } = string.Empty;
    }
}