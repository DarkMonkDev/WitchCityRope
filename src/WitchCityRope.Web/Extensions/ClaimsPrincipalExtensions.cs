using System.Security.Claims;

namespace WitchCityRope.Web.Extensions
{
    /// <summary>
    /// Extension methods for ClaimsPrincipal to simplify access to custom claims
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Gets the user's vetted status from claims
        /// </summary>
        public static bool IsVetted(this ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            var isVettedClaim = user.FindFirst("IsVetted");
            if (isVettedClaim != null && bool.TryParse(isVettedClaim.Value, out var isVetted))
            {
                return isVetted;
            }

            return false;
        }

        /// <summary>
        /// Gets the user's ID from claims
        /// </summary>
        public static Guid? GetUserId(this ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            return null;
        }

        /// <summary>
        /// Gets the user's scene name from claims
        /// </summary>
        public static string? GetSceneName(this ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            return user.FindFirst("SceneName")?.Value;
        }

        /// <summary>
        /// Gets the user's display name from claims
        /// </summary>
        public static string GetDisplayName(this ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return "Guest";

            return user.FindFirst("DisplayName")?.Value 
                ?? user.FindFirst("SceneName")?.Value 
                ?? user.FindFirst(ClaimTypes.Name)?.Value 
                ?? "Unknown";
        }

        /// <summary>
        /// Gets the user's role from claims
        /// </summary>
        public static string? GetUserRole(this ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            return user.FindFirst("UserRole")?.Value;
        }
    }
}