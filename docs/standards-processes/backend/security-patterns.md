# Security Best Practices

**Purpose**: Security patterns and practices for protecting user data and preventing vulnerabilities.
**When to Read**: When handling user input, authentication, authorization, or sensitive data.
**Related**: [Authentication Patterns](../development-standards/authentication-patterns.md), [Validation Standards](../validation-standardization/VALIDATION_STANDARDS.md)

## Input Validation and Sanitization

```csharp
/// <summary>
/// Validates and sanitizes vetting application input to prevent security
/// vulnerabilities while preserving data integrity. Applies multiple
/// layers of validation including format, content, and business rules.
///
/// Security measures implemented:
/// - Input length limits to prevent buffer overflow attacks
/// - HTML encoding to prevent XSS attacks
/// - SQL injection prevention through parameterized queries
/// - Business rule validation to prevent invalid state
/// - Audit logging for security monitoring
/// </summary>
public async Task<Result<VettingApplication>> SubmitVettingApplicationAsync(VettingApplicationRequest request)
{
    // Input validation - first line of defense
    if (request == null)
    {
        _logger.LogWarning("Vetting application submission attempted with null request");
        return Result<VettingApplication>.Failure("Invalid application data");
    }

    // Sanitize text inputs to prevent XSS attacks
    var sanitizedRequest = new VettingApplicationRequest
    {
        SceneName = SanitizeInput(request.SceneName, maxLength: 100),
        Email = SanitizeEmail(request.Email),
        Experience = SanitizeInput(request.Experience, maxLength: 2000),
        References = SanitizeInput(request.References, maxLength: 1000),
        ReasonForJoining = SanitizeInput(request.ReasonForJoining, maxLength: 2000)
    };

    // Business rule validation
    var validationResult = await _validator.ValidateAsync(sanitizedRequest);
    if (!validationResult.IsValid)
    {
        _logger.LogWarning("Vetting application failed validation: {Errors}",
            string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
        return Result<VettingApplication>.Failure("Application data is invalid");
    }

    // Create application entity with audit trail
    var application = new VettingApplication
    {
        SceneName = sanitizedRequest.SceneName,
        Email = sanitizedRequest.Email.ToLowerInvariant(), // Normalize email
        Experience = sanitizedRequest.Experience,
        References = sanitizedRequest.References,
        ReasonForJoining = sanitizedRequest.ReasonForJoining,
        SubmittedAt = DateTime.UtcNow,
        Status = VettingStatus.Pending,
        IpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
    };

    try
    {
        await _repository.CreateVettingApplicationAsync(application);

        // Log successful submission for audit trail
        _logger.LogInformation("Vetting application submitted successfully for {Email} from IP {IpAddress}",
            application.Email, application.IpAddress);

        return Result<VettingApplication>.Success(application);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to save vetting application for {Email}", request.Email);
        return Result<VettingApplication>.Failure("Could not submit application at this time");
    }
}

/// <summary>
/// Sanitizes text input to prevent XSS attacks while preserving readability.
/// Removes potentially dangerous HTML/script content and enforces length limits.
/// </summary>
private string SanitizeInput(string input, int maxLength)
{
    if (string.IsNullOrWhiteSpace(input))
        return string.Empty;

    // Remove potentially dangerous content
    var sanitized = HttpUtility.HtmlEncode(input.Trim());

    // Enforce length limits
    if (sanitized.Length > maxLength)
    {
        sanitized = sanitized.Substring(0, maxLength);
    }

    return sanitized;
}
```

## Security Checklist

### Input Security
- [ ] All user input is validated
- [ ] Input length limits enforced
- [ ] HTML/script content sanitized
- [ ] SQL injection prevented (use parameterized queries)
- [ ] File upload validation (type, size, content)

### Authentication & Authorization
- [ ] Authentication required for protected endpoints
- [ ] Authorization checks based on user roles
- [ ] Password requirements enforced (length, complexity)
- [ ] Session management secure (httpOnly cookies)
- [ ] Token expiration and refresh handled properly

### Data Protection
- [ ] Sensitive data encrypted at rest
- [ ] Sensitive data encrypted in transit (HTTPS)
- [ ] Personal information properly handled (GDPR, privacy)
- [ ] Audit logs for sensitive operations
- [ ] Secrets stored securely (Azure Key Vault, User Secrets)

### API Security
- [ ] Rate limiting implemented
- [ ] CORS configured properly
- [ ] API keys/tokens secured
- [ ] Error messages don't leak sensitive information
- [ ] API versioning supports security updates

## Common Vulnerabilities to Avoid

### SQL Injection
```csharp
// ❌ WRONG - Vulnerable to SQL injection
var query = $"SELECT * FROM Users WHERE Email = '{email}'";

// ✅ CORRECT - Parameterized query
var users = await _db.Users
    .Where(u => u.Email == email)
    .ToListAsync();
```

### XSS (Cross-Site Scripting)
```csharp
// ❌ WRONG - Unencoded user input
return $"<div>{userInput}</div>";

// ✅ CORRECT - HTML encoded
return $"<div>{HttpUtility.HtmlEncode(userInput)}</div>";
```

### Mass Assignment
```csharp
// ❌ WRONG - Allows setting any property
_db.Users.Update(user);

// ✅ CORRECT - Explicit property updates
user.SceneName = request.SceneName;
user.Pronouns = request.Pronouns;
// Don't allow setting IsAdmin, CreatedDate, etc.
```

### Insecure Direct Object References
```csharp
// ❌ WRONG - No authorization check
var application = await _db.VettingApplications.FindAsync(id);
return application;

// ✅ CORRECT - Verify user can access this resource
var application = await _db.VettingApplications
    .Where(a => a.Id == id && a.UserId == currentUserId)
    .FirstOrDefaultAsync();
```
