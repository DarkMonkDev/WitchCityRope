using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Core.Models.Auth
{
    /// <summary>
    /// Request model for refreshing an access token
    /// </summary>
    public class RefreshTokenRequest
    {
        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}