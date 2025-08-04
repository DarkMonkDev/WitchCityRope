using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WitchCityRope.Web.Shared.Validation.Services
{
    /// <summary>
    /// Service for centralized validation logic across the application
    /// </summary>
    public interface IValidationService
    {
        /// <summary>
        /// Checks if an email address is unique in the system
        /// </summary>
        /// <param name="email">Email address to check</param>
        /// <param name="excludeUserId">User ID to exclude from the check (for updates)</param>
        /// <returns>True if email is unique, false otherwise</returns>
        Task<bool> IsEmailUniqueAsync(string email, Guid? excludeUserId = null);

        /// <summary>
        /// Checks if a scene name is unique in the system
        /// </summary>
        /// <param name="sceneName">Scene name to check</param>
        /// <param name="excludeUserId">User ID to exclude from the check (for updates)</param>
        /// <returns>True if scene name is unique, false otherwise</returns>
        Task<bool> IsSceneNameUniqueAsync(string sceneName, Guid? excludeUserId = null);

        /// <summary>
        /// Validates email format and optionally checks DNS records
        /// </summary>
        /// <param name="email">Email address to validate</param>
        /// <returns>True if email is valid, false otherwise</returns>
        Task<bool> IsEmailValidAsync(string email);

        /// <summary>
        /// Validates password strength against complexity requirements
        /// </summary>
        /// <param name="password">Password to validate</param>
        /// <param name="errors">List of validation errors if any</param>
        /// <returns>True if password meets requirements, false otherwise</returns>
        bool IsPasswordValid(string password, out List<string> errors);

        /// <summary>
        /// Validates scene name format and content
        /// </summary>
        /// <param name="sceneName">Scene name to validate</param>
        /// <param name="error">Error message if validation fails</param>
        /// <returns>True if scene name is valid, false otherwise</returns>
        bool IsSceneNameValid(string sceneName, out string error);

        /// <summary>
        /// Validates phone number format
        /// </summary>
        /// <param name="phoneNumber">Phone number to validate</param>
        /// <returns>True if phone number is valid, false otherwise</returns>
        bool IsPhoneNumberValid(string phoneNumber);

        /// <summary>
        /// Validates date is in the future
        /// </summary>
        /// <param name="date">Date to validate</param>
        /// <param name="minimumDaysInFuture">Minimum days the date must be in the future</param>
        /// <returns>True if date is valid, false otherwise</returns>
        bool IsDateInFuture(DateTime date, int minimumDaysInFuture = 0);

        /// <summary>
        /// Validates age meets minimum requirement
        /// </summary>
        /// <param name="dateOfBirth">Date of birth</param>
        /// <param name="minimumAge">Minimum age required (default 21)</param>
        /// <returns>True if age meets requirement, false otherwise</returns>
        bool MeetsAgeRequirement(DateTime dateOfBirth, int minimumAge = 21);

        /// <summary>
        /// Validates credit card number using Luhn algorithm
        /// </summary>
        /// <param name="cardNumber">Credit card number</param>
        /// <returns>True if card number is valid, false otherwise</returns>
        bool IsValidCreditCardNumber(string cardNumber);

        /// <summary>
        /// Validates URL format
        /// </summary>
        /// <param name="url">URL to validate</param>
        /// <returns>True if URL is valid, false otherwise</returns>
        bool IsValidUrl(string url);
    }
}