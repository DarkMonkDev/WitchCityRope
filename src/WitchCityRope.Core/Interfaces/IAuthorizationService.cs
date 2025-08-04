using System.Security.Claims;

namespace WitchCityRope.Core.Interfaces
{
    public interface IAuthorizationService
    {
        bool IsAuthenticated(ClaimsPrincipal? user);
        bool IsInRole(ClaimsPrincipal? user, string role);
        bool CanAccessEndpoint(ClaimsPrincipal? user, string endpoint);
        bool IsPublicEndpoint(string endpoint);
    }
}