using System;
using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Core.Models.Auth
{
    /// <summary>
    /// Request model for email verification
    /// </summary>
    public class VerifyEmailRequest
    {
        [Required(ErrorMessage = "User ID is required")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Verification token is required")]
        public string Token { get; set; } = string.Empty;
    }
}