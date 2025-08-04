using Microsoft.AspNetCore.Identity;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Web.Components.Account;

internal sealed class IdentityUserAccessor(
    UserManager<WitchCityRopeUser> userManager,
    IdentityRedirectManager redirectManager)
{
    public async Task<WitchCityRopeUser> GetRequiredUserAsync(HttpContext context)
    {
        var user = await userManager.GetUserAsync(context.User);

        if (user is null)
        {
            redirectManager.RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
        }

        return user;
    }
}