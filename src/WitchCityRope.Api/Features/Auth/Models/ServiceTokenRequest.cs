using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Auth.Models
{
    /// <summary>
    /// Request model for service-to-service authentication
    /// Used by the Web service to get JWT tokens for authenticated users
    /// </summary>
    public class ServiceTokenRequest
    {
        [Required(ErrorMessage = "UserId is required")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
    }
}