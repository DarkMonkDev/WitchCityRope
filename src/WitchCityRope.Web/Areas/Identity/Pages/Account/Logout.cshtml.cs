using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Web.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<WitchCityRopeUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(SignInManager<WitchCityRopeUser> signInManager, ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnPost(string? returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                // This needs to be replaced with a redirect to your home page.
                return RedirectToPage("/");
            }
        }
        
        public async Task<IActionResult> OnGet()
        {
            // For GET requests, sign out and redirect
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out via GET request.");
            return LocalRedirect("~/");
        }
    }
}