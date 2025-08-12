using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Auth.Models
{
    /// <summary>
    /// Request model for web service authentication (user already authenticated in Web service)
    /// </summary>
    public class WebServiceLoginRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Authentication type is required")]
        public string AuthenticationType { get; set; } = string.Empty;

        [Required(ErrorMessage = "User ID is required")]
        public string UserId { get; set; } = string.Empty;
    }
}