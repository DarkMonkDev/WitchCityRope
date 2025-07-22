using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<WitchCityRopeUser> _signInManager;
        private readonly UserManager<WitchCityRopeUser> _userManager;
        private readonly IUserStore<WitchCityRopeUser> _userStore;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<WitchCityRopeUser> userManager,
            IUserStore<WitchCityRopeUser> userStore,
            SignInManager<WitchCityRopeUser> signInManager,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _userStore = userStore;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
            [Display(Name = "Scene Name")]
            public string SceneName { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task OnGetAsync(string? returnUrl = null, string? email = null, string? sceneName = null)
        {
            ReturnUrl = returnUrl;
            
            // Pre-fill form from query parameters if provided
            if (!string.IsNullOrEmpty(email))
            {
                Input.Email = email;
            }
            
            if (!string.IsNullOrEmpty(sceneName))
            {
                Input.SceneName = sceneName;
            }
            
            await Task.CompletedTask;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            
            if (ModelState.IsValid)
            {
                // Create user with parameterless constructor
                var user = new WitchCityRopeUser();
                user.UserName = Input.Email;
                user.Email = Input.Email;
                user.EmailConfirmed = false;
                
                // Set scene name using the update method after user is created
                var sceneNameValue = Core.ValueObjects.SceneName.Create(Input.SceneName);
                
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");
                    
                    // Update the scene name after user is created
                    user.UpdateSceneName(sceneNameValue);
                    await _userManager.UpdateAsync(user);

                    // Assign default role
                    await _userManager.AddToRoleAsync(user, "Member");

                    // Sign in the user
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    
                    return LocalRedirect(returnUrl);
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}