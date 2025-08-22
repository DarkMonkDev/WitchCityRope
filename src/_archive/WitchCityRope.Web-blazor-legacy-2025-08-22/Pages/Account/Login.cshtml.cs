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

            // Return the page which will handle the client-side redirect
            return Page();
        }
    }
}