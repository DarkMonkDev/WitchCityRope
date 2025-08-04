using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WitchCityRope.Web.Pages.Account
{
    public class LoginModel : PageModel
    {
        public string? ReturnUrl { get; set; }

        public IActionResult OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
            
            // For authenticated users, redirect to the return URL or home
            if (User.Identity?.IsAuthenticated == true)
            {
                return LocalRedirect(returnUrl ?? "/");
            }

            // Immediately redirect to the new Identity login page
            return Redirect($"/Identity/Account/Login{(returnUrl != null ? "?returnUrl=" + Uri.EscapeDataString(returnUrl) : "")}");
        }
    }
}