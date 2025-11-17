using Microsoft.AspNetCore.Http;

namespace WitchCityRope.Api.Features.Authentication.Services;

/// <summary>
/// Interface for OWASP-compliant URL validation service to prevent open redirect attacks
/// Implements strict validation rules per business requirements BR-1 and SEC-1
/// </summary>
public interface IReturnUrlValidator
{
    /// <summary>
    /// Validates a return URL against OWASP security standards
    /// Implements comprehensive checks to prevent open redirect attacks
    /// </summary>
    /// <param name="returnUrl">The URL to validate (can be relative or absolute)</param>
    /// <param name="httpContext">Current HTTP context for host validation</param>
    /// <param name="userId">User ID for audit logging (optional)</param>
    /// <returns>ValidationResult containing success status, validated URL, and audit details</returns>
    ReturnUrlValidationResult ValidateReturnUrl(
        string? returnUrl,
        HttpContext httpContext,
        string? userId = null);
}
