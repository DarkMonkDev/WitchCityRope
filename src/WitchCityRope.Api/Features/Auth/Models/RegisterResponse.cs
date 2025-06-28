using System;

namespace WitchCityRope.Api.Features.Auth.Models
{
    /// <summary>
    /// Response model for successful registration
    /// </summary>
    public class RegisterResponse
    {
        /// <summary>
        /// The newly created user's ID
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The registered email address
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Whether the verification email was sent successfully
        /// </summary>
        public bool EmailVerificationSent { get; set; }

        /// <summary>
        /// Success message for the user
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}