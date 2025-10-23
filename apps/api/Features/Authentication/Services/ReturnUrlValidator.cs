using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WitchCityRope.Api.Features.Authentication.Services;

/// <summary>
/// OWASP-compliant URL validation service to prevent open redirect attacks
/// Implements strict validation rules per business requirements BR-1 and SEC-1
///
/// Security principles enforced:
/// - Protocol validation (only http/https allowed)
/// - Domain validation (must match application domain)
/// - Path allow-list validation
/// - Preferential treatment of relative URLs
/// - Comprehensive audit logging
///
/// References:
/// - OWASP Unvalidated Redirects and Forwards Cheat Sheet
/// - BR-1: Return URL Validation (CRITICAL SECURITY)
/// - SEC-1: Open Redirect Prevention
/// </summary>
public class ReturnUrlValidator
{
    private readonly ILogger<ReturnUrlValidator> _logger;
    private readonly IConfiguration _configuration;
    private readonly string[] _allowedDomains;
    private readonly HashSet<string> _allowedPaths;

    public ReturnUrlValidator(
        ILogger<ReturnUrlValidator> _logger,
        IConfiguration configuration)
    {
        this._logger = _logger;
        _configuration = configuration;

        // Load allowed domains from configuration
        _allowedDomains = configuration.GetSection("Authentication:AllowedDomains")
            .Get<string[]>() ?? new[] { "localhost", "witchcityrope.com" };

        // Initialize allow-list of permitted application routes
        // These represent legitimate application pages users can be redirected to
        _allowedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Dashboard and profile
            "/dashboard",
            "/profile",

            // Vetting application
            "/apply/vetting",
            "/vetting/application",

            // Events
            "/events",
            "/events/details",

            // Demo pages
            "/demo/event-session-matrix",

            // Public pages
            "/about",
            "/contact",

            // Root is allowed (will default to dashboard for authenticated users)
            "/"
        };
    }

    /// <summary>
    /// Validates a return URL against OWASP security standards
    /// Implements comprehensive checks to prevent open redirect attacks
    /// </summary>
    /// <param name="returnUrl">The URL to validate (can be relative or absolute)</param>
    /// <param name="httpContext">Current HTTP context for host validation</param>
    /// <param name="userId">User ID for audit logging (optional)</param>
    /// <returns>ValidationResult containing success status, validated URL, and audit details</returns>
    public ReturnUrlValidationResult ValidateReturnUrl(
        string? returnUrl,
        HttpContext httpContext,
        string? userId = null)
    {
        var validationContext = new ValidationContext
        {
            OriginalUrl = returnUrl,
            UserId = userId,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers["User-Agent"].ToString(),
            Timestamp = DateTime.UtcNow
        };

        try
        {
            // Step 1: Null/empty check
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return CreateFailureResult(
                    validationContext,
                    "Empty return URL",
                    "Return URL is null or empty - using default dashboard redirect");
            }

            // Step 2: Trim and normalize
            returnUrl = returnUrl.Trim();

            // Step 3: Check for dangerous protocols (OWASP requirement)
            if (ContainsDangerousProtocol(returnUrl))
            {
                return CreateFailureResult(
                    validationContext with { OriginalUrl = returnUrl },
                    "Dangerous protocol detected",
                    $"Blocked dangerous protocol in URL: {returnUrl}",
                    isMalicious: true);
            }

            // Step 4: Prefer relative URLs (OWASP best practice)
            if (IsRelativeUrl(returnUrl))
            {
                // Validate relative path against allow-list
                if (IsPathAllowed(returnUrl))
                {
                    return CreateSuccessResult(
                        validationContext with { OriginalUrl = returnUrl },
                        returnUrl,
                        "Relative URL validated successfully");
                }

                // Check if path starts with an allowed path (e.g., /events/123)
                if (IsPathPrefixAllowed(returnUrl))
                {
                    return CreateSuccessResult(
                        validationContext with { OriginalUrl = returnUrl },
                        returnUrl,
                        "Relative URL with allowed prefix validated successfully");
                }

                return CreateFailureResult(
                    validationContext with { OriginalUrl = returnUrl },
                    "Path not in allow-list",
                    $"Relative URL path not allowed: {returnUrl}");
            }

            // Step 5: Parse absolute URL and validate
            if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
            {
                return CreateFailureResult(
                    validationContext with { OriginalUrl = returnUrl },
                    "Invalid URL format",
                    $"Could not parse URL: {returnUrl}");
            }

            // Step 6: Protocol validation (only http/https allowed)
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                return CreateFailureResult(
                    validationContext with { OriginalUrl = returnUrl },
                    "Invalid protocol",
                    $"Only http/https protocols allowed, got: {uri.Scheme}",
                    isMalicious: true);
            }

            // Step 7: Domain validation (must match application domain)
            var requestHost = httpContext.Request.Host.Host.ToLowerInvariant();
            var targetHost = uri.Host.ToLowerInvariant();

            if (!IsDomainAllowed(targetHost, requestHost))
            {
                return CreateFailureResult(
                    validationContext with { OriginalUrl = returnUrl },
                    "External domain blocked",
                    $"Domain {targetHost} not in allowed list (request from {requestHost})",
                    isMalicious: true);
            }

            // Step 8: Port validation (prevent port-based attacks)
            var requestPort = httpContext.Request.Host.Port;
            if (uri.Port != requestPort && !IsStandardPort(uri))
            {
                _logger.LogWarning(
                    "Port mismatch in return URL - Request: {RequestPort}, Target: {TargetPort}, URL: {Url}",
                    requestPort, uri.Port, returnUrl);
            }

            // Step 9: Path validation for absolute URLs
            var pathAndQuery = uri.PathAndQuery;
            if (!IsPathAllowed(uri.AbsolutePath) && !IsPathPrefixAllowed(uri.AbsolutePath))
            {
                return CreateFailureResult(
                    validationContext with { OriginalUrl = returnUrl },
                    "Absolute URL path not allowed",
                    $"Path {uri.AbsolutePath} not in allow-list");
            }

            // All validations passed - convert to relative URL for safety
            var safeRelativeUrl = pathAndQuery;
            return CreateSuccessResult(
                validationContext with { OriginalUrl = returnUrl },
                safeRelativeUrl,
                $"Absolute URL validated and converted to relative: {safeRelativeUrl}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during return URL validation for {Url}", returnUrl);
            return CreateFailureResult(
                validationContext with { OriginalUrl = returnUrl },
                "Validation exception",
                $"Error validating URL: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks for dangerous protocols that could enable XSS attacks
    /// </summary>
    private bool ContainsDangerousProtocol(string url)
    {
        var dangerousProtocols = new[] { "javascript:", "data:", "file:", "vbscript:", "about:" };
        var lowerUrl = url.ToLowerInvariant();

        foreach (var protocol in dangerousProtocols)
        {
            if (lowerUrl.StartsWith(protocol))
            {
                _logger.LogWarning(
                    "SECURITY: Dangerous protocol detected in return URL: {Protocol} in {Url}",
                    protocol, url);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if URL is relative (starts with / but not //)
    /// </summary>
    private bool IsRelativeUrl(string url)
    {
        return url.StartsWith("/") && !url.StartsWith("//");
    }

    /// <summary>
    /// Checks if path exactly matches an entry in the allow-list
    /// </summary>
    private bool IsPathAllowed(string path)
    {
        // Extract path without query string
        var pathWithoutQuery = path.Split('?')[0];
        return _allowedPaths.Contains(pathWithoutQuery);
    }

    /// <summary>
    /// Checks if path starts with an allowed prefix (e.g., /events/ matches /events/123)
    /// This allows dynamic routes like event details pages
    /// </summary>
    private bool IsPathPrefixAllowed(string path)
    {
        // Extract path without query string
        var pathWithoutQuery = path.Split('?')[0];

        // Define allowed prefixes for dynamic routes
        var allowedPrefixes = new[]
        {
            "/events/",
            "/demo/",
            "/vetting/",
            "/profile/"
        };

        foreach (var prefix in allowedPrefixes)
        {
            if (pathWithoutQuery.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if domain is in the allowed list
    /// </summary>
    private bool IsDomainAllowed(string targetHost, string requestHost)
    {
        // Exact match with request host (same-origin)
        if (targetHost == requestHost)
        {
            return true;
        }

        // Check against configured allowed domains
        foreach (var allowedDomain in _allowedDomains)
        {
            if (targetHost == allowedDomain || targetHost.EndsWith($".{allowedDomain}"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if port is standard HTTP/HTTPS port
    /// </summary>
    private bool IsStandardPort(Uri uri)
    {
        return (uri.Scheme == Uri.UriSchemeHttp && uri.Port == 80) ||
               (uri.Scheme == Uri.UriSchemeHttps && uri.Port == 443);
    }

    /// <summary>
    /// Creates a success validation result with audit logging
    /// </summary>
    private ReturnUrlValidationResult CreateSuccessResult(
        ValidationContext context,
        string validatedUrl,
        string reason)
    {
        _logger.LogInformation(
            "Return URL validation SUCCESS: User={UserId}, OriginalUrl={OriginalUrl}, ValidatedUrl={ValidatedUrl}, Reason={Reason}, IP={IpAddress}",
            context.UserId ?? "Anonymous",
            context.OriginalUrl,
            validatedUrl,
            reason,
            context.IpAddress);

        return new ReturnUrlValidationResult
        {
            IsValid = true,
            ValidatedUrl = validatedUrl,
            OriginalUrl = context.OriginalUrl,
            FailureReason = null,
            Timestamp = context.Timestamp,
            UserId = context.UserId,
            IpAddress = context.IpAddress
        };
    }

    /// <summary>
    /// Creates a failure validation result with security audit logging
    /// </summary>
    private ReturnUrlValidationResult CreateFailureResult(
        ValidationContext context,
        string category,
        string reason,
        bool isMalicious = false)
    {
        var logLevel = isMalicious ? LogLevel.Warning : LogLevel.Information;

        _logger.Log(
            logLevel,
            "Return URL validation FAILURE: User={UserId}, OriginalUrl={OriginalUrl}, Category={Category}, Reason={Reason}, IP={IpAddress}, UserAgent={UserAgent}, Malicious={IsMalicious}",
            context.UserId ?? "Anonymous",
            context.OriginalUrl,
            category,
            reason,
            context.IpAddress,
            context.UserAgent,
            isMalicious);

        return new ReturnUrlValidationResult
        {
            IsValid = false,
            ValidatedUrl = null,
            OriginalUrl = context.OriginalUrl,
            FailureReason = reason,
            Timestamp = context.Timestamp,
            UserId = context.UserId,
            IpAddress = context.IpAddress
        };
    }

    /// <summary>
    /// Internal context record for validation tracking
    /// </summary>
    private record ValidationContext
    {
        public string? OriginalUrl { get; init; }
        public string? UserId { get; init; }
        public string? IpAddress { get; init; }
        public string? UserAgent { get; init; }
        public DateTime Timestamp { get; init; }
    }
}

/// <summary>
/// Result of return URL validation with audit trail
/// </summary>
public record ReturnUrlValidationResult
{
    /// <summary>
    /// Indicates if the URL passed all validation checks
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// The validated URL (converted to relative if possible) if validation succeeded, null otherwise
    /// </summary>
    public string? ValidatedUrl { get; init; }

    /// <summary>
    /// The original URL that was submitted for validation
    /// </summary>
    public string? OriginalUrl { get; init; }

    /// <summary>
    /// Explanation of why validation failed (null if successful)
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// Timestamp when validation occurred (UTC)
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// User ID for audit trail (if available)
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// IP address for security monitoring
    /// </summary>
    public string? IpAddress { get; init; }
}
