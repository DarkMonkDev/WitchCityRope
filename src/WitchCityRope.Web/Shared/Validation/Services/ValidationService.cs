using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Infrastructure.Identity;

namespace WitchCityRope.Web.Shared.Validation.Services
{
    /// <summary>
    /// Implementation of validation service for centralized validation logic
    /// </summary>
    public class ValidationService : IValidationService
    {
        private readonly UserManager<WitchCityRopeUser> _userManager;
        private static readonly Regex SceneNameRegex = new(@"^[a-zA-Z0-9\s\-_'\.]+$", RegexOptions.Compiled);
        private static readonly Regex PhoneRegex = new(@"^[\+]?[(]?[0-9]{3}[)]?[-\s\.]?[(]?[0-9]{3}[)]?[-\s\.]?[0-9]{4,6}$", RegexOptions.Compiled);
        private static readonly Regex UrlRegex = new(@"^https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)$", RegexOptions.Compiled);

        public ValidationService(UserManager<WitchCityRopeUser> userManager)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public async Task<bool> IsEmailUniqueAsync(string email, Guid? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var query = _userManager.Users.Where(u => u.Email == email);
            
            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<bool> IsSceneNameUniqueAsync(string sceneName, Guid? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return false;

            // Get all users and do case-insensitive comparison in memory
            // This is acceptable for scene name uniqueness checks as user count is limited
            var users = await _userManager.Users.ToListAsync();
            
            var normalizedSceneName = sceneName.Trim();
            var matchingUsers = users.Where(u => 
                string.Equals(u.SceneName, normalizedSceneName, StringComparison.OrdinalIgnoreCase));
            
            if (excludeUserId.HasValue)
            {
                matchingUsers = matchingUsers.Where(u => u.Id != excludeUserId.Value);
            }

            return !matchingUsers.Any();
        }

        public Task<bool> IsEmailValidAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Task.FromResult(false);

            try
            {
                var mailAddress = new MailAddress(email);
                
                // Basic format validation passed, could add DNS checking here
                // For now, just check format
                return Task.FromResult(mailAddress.Address == email);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public bool IsPasswordValid(string password, out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Password is required");
                return false;
            }

            if (password.Length < 8)
            {
                errors.Add("Password must be at least 8 characters long");
            }

            if (!password.Any(char.IsUpper))
            {
                errors.Add("Password must contain at least one uppercase letter");
            }

            if (!password.Any(char.IsLower))
            {
                errors.Add("Password must contain at least one lowercase letter");
            }

            if (!password.Any(char.IsDigit))
            {
                errors.Add("Password must contain at least one number");
            }

            if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                errors.Add("Password must contain at least one special character");
            }

            return errors.Count == 0;
        }

        public bool IsSceneNameValid(string sceneName, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                error = "Scene name is required";
                return false;
            }

            if (sceneName.Length < 2)
            {
                error = "Scene name must be at least 2 characters long";
                return false;
            }

            if (sceneName.Length > 50)
            {
                error = "Scene name must be less than 50 characters";
                return false;
            }

            if (!SceneNameRegex.IsMatch(sceneName))
            {
                error = "Scene name can only contain letters, numbers, spaces, hyphens, underscores, apostrophes, and periods";
                return false;
            }

            // Check for inappropriate content
            var lowerSceneName = sceneName.ToLowerInvariant();
            var inappropriateTerms = new[] { "admin", "administrator", "moderator", "staff", "official" };
            
            if (inappropriateTerms.Any(term => lowerSceneName.Contains(term)))
            {
                error = "Scene name contains reserved terms";
                return false;
            }

            return true;
        }

        public bool IsPhoneNumberValid(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Remove common formatting characters
            var cleaned = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            
            // Basic validation - adjust regex as needed for your requirements
            return PhoneRegex.IsMatch(cleaned);
        }

        public bool IsDateInFuture(DateTime date, int minimumDaysInFuture = 0)
        {
            var minimumDate = DateTime.UtcNow.Date.AddDays(minimumDaysInFuture);
            return date.Date >= minimumDate;
        }

        public bool MeetsAgeRequirement(DateTime dateOfBirth, int minimumAge = 21)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            
            // Adjust if birthday hasn't occurred this year
            if (dateOfBirth.Date > today.AddYears(-age))
            {
                age--;
            }

            return age >= minimumAge;
        }

        public bool IsValidCreditCardNumber(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
                return false;

            // Remove spaces and dashes
            cardNumber = cardNumber.Replace(" ", "").Replace("-", "");

            // Check if all characters are digits
            if (!cardNumber.All(char.IsDigit))
                return false;

            // Check length (most cards are 13-19 digits)
            if (cardNumber.Length < 13 || cardNumber.Length > 19)
                return false;

            // Luhn algorithm
            int sum = 0;
            bool isEven = false;

            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                int digit = cardNumber[i] - '0';

                if (isEven)
                {
                    digit *= 2;
                    if (digit > 9)
                    {
                        digit -= 9;
                    }
                }

                sum += digit;
                isEven = !isEven;
            }

            return sum % 10 == 0;
        }

        public bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            // Try to create a Uri object
            if (Uri.TryCreate(url, UriKind.Absolute, out var uriResult))
            {
                return uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps;
            }

            return false;
        }
    }
}