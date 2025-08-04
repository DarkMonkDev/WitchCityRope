using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WitchCityRope.Infrastructure.Identity;

namespace Microsoft.AspNetCore.Routing;

internal static class IdentityComponentsEndpointRouteBuilderExtensions
{
    // These endpoints are required by the Identity Razor components defined in the /Components/Account/Pages directory of this project.
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var accountGroup = endpoints.MapGroup("/Account");

        accountGroup.MapPost("/Login", async (
            [FromServices] UserManager<WitchCityRopeUser> userManager,
            [FromServices] SignInManager<WitchCityRopeUser> signInManager,
            [FromServices] ILogger<Program> logger,
            [FromForm] string emailOrUsername,
            [FromForm] string password,
            [FromForm] bool rememberMe,
            [FromForm] string? returnUrl) =>
        {
            // Try to find user by email first, then by username
            var user = await userManager.FindByEmailAsync(emailOrUsername);
            if (user == null)
            {
                user = await userManager.FindByNameAsync(emailOrUsername);
            }

            if (user == null)
            {
                return TypedResults.LocalRedirect($"~/login?error=invalid");
            }

            var result = await signInManager.PasswordSignInAsync(
                user.UserName, 
                password, 
                rememberMe, 
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                logger.LogInformation("Login successful for user: {UserId}", user.Id);
                user.LastLoginAt = DateTime.UtcNow;
                await userManager.UpdateAsync(user);
                
                var redirectUrl = !string.IsNullOrEmpty(returnUrl) ? returnUrl : "/member/dashboard";
                return TypedResults.LocalRedirect($"~{redirectUrl}");
            }
            else if (result.RequiresTwoFactor)
            {
                return TypedResults.LocalRedirect($"~/auth/two-factor?email={Uri.EscapeDataString(emailOrUsername)}");
            }
            else
            {
                return TypedResults.LocalRedirect($"~/login?error=invalid");
            }
        });

        accountGroup.MapPost("/Logout", async (
            ClaimsPrincipal user,
            [FromServices] SignInManager<WitchCityRopeUser> signInManager,
            [FromForm] string returnUrl) =>
        {
            await signInManager.SignOutAsync();
            return TypedResults.LocalRedirect($"~/{returnUrl}");
        });

        return accountGroup;
    }
}