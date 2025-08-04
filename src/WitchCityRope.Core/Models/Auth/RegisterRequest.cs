using System;
using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Core.Models.Auth
{
    /// <summary>
    /// Request model for user registration
    /// </summary>
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]", 
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Scene name is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Scene name must be between 3 and 50 characters")]
        public string SceneName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Legal name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Legal name must be between 2 and 100 characters")]
        public string LegalName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [StringLength(100, ErrorMessage = "Pronounced name cannot exceed 100 characters")]
        public string? PronouncedName { get; set; }

        [StringLength(50, ErrorMessage = "Pronouns cannot exceed 50 characters")]
        public string? Pronouns { get; set; }
    }
}