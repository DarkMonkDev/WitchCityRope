# Security Implementation Guide

This guide provides practical implementation details for the security features in the WitchCityRope platform.

## Table of Contents
1. [Authentication Implementation](#authentication-implementation)
2. [Data Encryption Examples](#data-encryption-examples)
3. [Input Validation Patterns](#input-validation-patterns)
4. [Security Headers Configuration](#security-headers-configuration)
5. [Common Security Patterns](#common-security-patterns)

## Authentication Implementation

### JWT Token Generation

```csharp
// Example: Generating a secure JWT token
public string GenerateToken(User user)
{
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes(_secretKey);
    
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email.Value),
        new Claim(ClaimTypes.Name, user.SceneName.Value),
        new Claim(ClaimTypes.Role, user.Role.ToString()),
        new Claim("UserId", user.Id.ToString()),
        new Claim("SceneName", user.SceneName.Value)
    };
    
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddMinutes(_expirationMinutes),
        Issuer = _issuer,
        Audience = _audience,
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256Signature)
    };
    
    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
}
```

### Password Hashing with BCrypt

```csharp
// Example: Secure password hashing
public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12; // BCrypt work factor
    
    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentNullException(nameof(password));
            
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }
    
    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
            return false;
            
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}
```

### Secure Login Implementation

```csharp
public async Task<LoginResponse> LoginAsync(LoginRequest request)
{
    // Rate limiting check
    if (!await _rateLimiter.CheckLoginAttemptAsync(request.Email))
    {
        throw new TooManyAttemptsException("Too many login attempts. Please try again later.");
    }
    
    // Validate user credentials
    var user = await _userRepository.GetByEmailAsync(request.Email);
    if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
    {
        await _rateLimiter.RecordFailedLoginAsync(request.Email);
        throw new UnauthorizedException("Invalid email or password");
    }
    
    // Check account status
    if (!user.IsActive)
        throw new UnauthorizedException("Account is not active");
        
    if (!user.EmailVerified)
        throw new UnauthorizedException("Please verify your email address");
    
    // Check for 2FA requirement
    if (user.TwoFactorEnabled)
    {
        var twoFactorToken = GenerateTwoFactorToken();
        await _cache.SetAsync($"2fa:{user.Id}", twoFactorToken, TimeSpan.FromMinutes(5));
        return new LoginResponse { RequiresTwoFactor = true, TwoFactorToken = twoFactorToken };
    }
    
    // Generate tokens
    var jwt = GenerateToken(user);
    var refreshToken = GenerateRefreshToken();
    
    // Store refresh token
    await _userRepository.StoreRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.AddDays(7));
    
    // Update last login
    await _userRepository.UpdateLastLoginAsync(user.Id);
    
    // Clear failed login attempts
    await _rateLimiter.ClearFailedAttemptsAsync(request.Email);
    
    return new LoginResponse
    {
        Token = jwt,
        RefreshToken = refreshToken,
        User = MapToUserDto(user)
    };
}
```

## Data Encryption Examples

### Field-Level Encryption

```csharp
public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;
    
    public EncryptionService(IConfiguration configuration)
    {
        var encryptionKey = configuration["Encryption:Key"];
        using (var sha256 = SHA256.Create())
        {
            var fullKey = sha256.ComputeHash(Encoding.UTF8.GetBytes(encryptionKey));
            _key = new byte[32];
            Array.Copy(fullKey, 0, _key, 0, 32);
            
            var ivHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(encryptionKey + ":IV"));
            _iv = new byte[16];
            Array.Copy(ivHash, 0, _iv, 0, 16);
        }
    }
    
    public async Task<string> EncryptAsync(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;
            
        using (var aes = Aes.Create())
        {
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            
            using (var encryptor = aes.CreateEncryptor())
            using (var msEncrypt = new MemoryStream())
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                await swEncrypt.WriteAsync(plainText);
                await swEncrypt.FlushAsync();
                csEncrypt.FlushFinalBlock();
                return Convert.ToBase64String(msEncrypt.ToArray());
            }
        }
    }
}
```

### Entity Configuration with Encryption

```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    private readonly IEncryptionService _encryptionService;
    
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Encrypt legal name on save, decrypt on load
        builder.Property(u => u.EncryptedLegalName)
            .HasConversion(
                v => _encryptionService.EncryptAsync(v).Result,
                v => _encryptionService.DecryptAsync(v).Result
            );
            
        // Other sensitive fields...
    }
}
```

## Input Validation Patterns

### Comprehensive Request Validation

```csharp
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .Must(BeValidEmailDomain).WithMessage("Email domain is not allowed");
            
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain number")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain special character")
            .Must(NotBeCommonPassword).WithMessage("Password is too common");
            
        RuleFor(x => x.SceneName)
            .NotEmpty().WithMessage("Scene name is required")
            .Length(3, 50).WithMessage("Scene name must be between 3 and 50 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_]+$").WithMessage("Scene name contains invalid characters")
            .Must(NotContainProfanity).WithMessage("Scene name contains inappropriate content");
            
        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required")
            .Must(BeAtLeast21YearsOld).WithMessage("Must be at least 21 years old");
    }
    
    private bool BeValidEmailDomain(string email)
    {
        if (string.IsNullOrEmpty(email)) return false;
        
        var blockedDomains = new[] { "tempmail.com", "throwaway.email" };
        var domain = email.Split('@').LastOrDefault()?.ToLower();
        
        return !string.IsNullOrEmpty(domain) && !blockedDomains.Contains(domain);
    }
    
    private bool NotBeCommonPassword(string password)
    {
        var commonPasswords = new HashSet<string>
        {
            "password", "12345678", "qwerty", "abc123", "password123"
        };
        
        return !commonPasswords.Contains(password.ToLower());
    }
    
    private bool BeAtLeast21YearsOld(DateTime dateOfBirth)
    {
        var age = DateTime.UtcNow.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > DateTime.UtcNow.AddYears(-age)) age--;
        return age >= 21;
    }
}
```

### Sanitization Middleware

```csharp
public class InputSanitizationMiddleware
{
    private readonly RequestDelegate _next;
    
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.HasFormContentType)
        {
            var form = await context.Request.ReadFormAsync();
            var sanitizedForm = new Dictionary<string, StringValues>();
            
            foreach (var field in form)
            {
                sanitizedForm[field.Key] = SanitizeInput(field.Value);
            }
            
            context.Request.Form = new FormCollection(sanitizedForm);
        }
        
        await _next(context);
    }
    
    private string SanitizeInput(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        
        // Remove null bytes
        input = input.Replace("\0", "");
        
        // Trim whitespace
        input = input.Trim();
        
        // Additional sanitization as needed...
        
        return input;
    }
}
```

## Security Headers Configuration

### Comprehensive Security Headers

```csharp
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Prevent XSS attacks
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        
        // Prevent MIME type sniffing
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        
        // Prevent clickjacking
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        
        // Control referrer information
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        
        // Content Security Policy
        context.Response.Headers.Add("Content-Security-Policy", 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.syncfusion.com; " +
            "style-src 'self' 'unsafe-inline' https://cdn.syncfusion.com; " +
            "img-src 'self' data: https:; " +
            "font-src 'self' https://cdn.syncfusion.com; " +
            "connect-src 'self' https://api.stripe.com; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'");
        
        // Permissions Policy
        context.Response.Headers.Add("Permissions-Policy", 
            "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=*, usb=()");
        
        // HSTS for HTTPS connections
        if (context.Request.IsHttps)
        {
            context.Response.Headers.Add("Strict-Transport-Security", 
                "max-age=31536000; includeSubDomains; preload");
        }
        
        await _next(context);
    }
}
```

## Common Security Patterns

### Secure File Upload

```csharp
public class SecureFileUploadService
{
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };
    private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB
    
    public async Task<string> UploadFileAsync(IFormFile file, Guid userId)
    {
        // Validate file
        if (file == null || file.Length == 0)
            throw new ValidationException("No file provided");
            
        if (file.Length > _maxFileSize)
            throw new ValidationException($"File size exceeds {_maxFileSize / 1024 / 1024}MB limit");
            
        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!_allowedExtensions.Contains(extension))
            throw new ValidationException("File type not allowed");
            
        // Verify file content matches extension
        if (!await VerifyFileSignature(file, extension))
            throw new ValidationException("File content does not match extension");
            
        // Generate secure filename
        var fileName = $"{userId}_{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_uploadPath, fileName);
        
        // Scan for viruses
        if (!await ScanForViruses(file))
            throw new SecurityException("File failed security scan");
            
        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
        
        return fileName;
    }
    
    private async Task<bool> VerifyFileSignature(IFormFile file, string extension)
    {
        var signatures = new Dictionary<string, byte[]>
        {
            { ".jpg", new byte[] { 0xFF, 0xD8, 0xFF } },
            { ".jpeg", new byte[] { 0xFF, 0xD8, 0xFF } },
            { ".png", new byte[] { 0x89, 0x50, 0x4E, 0x47 } },
            { ".pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 } }
        };
        
        if (!signatures.ContainsKey(extension))
            return false;
            
        var expectedSignature = signatures[extension];
        var buffer = new byte[expectedSignature.Length];
        
        file.Position = 0;
        await file.ReadAsync(buffer, 0, buffer.Length);
        file.Position = 0;
        
        return buffer.SequenceEqual(expectedSignature);
    }
}
```

### CSRF Protection Implementation

```csharp
// In Startup.cs or Program.cs
services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "CSRF-TOKEN";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// In API Controller
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
{
    // Your code here
}

// For Blazor components
@inject IAntiforgery Antiforgery

@code {
    protected override async Task OnInitializedAsync()
    {
        var token = Antiforgery.GetAndStoreTokens(HttpContext).RequestToken;
        // Include token in API calls
    }
}
```

### Rate Limiting Implementation

```csharp
public class RateLimitingService
{
    private readonly IMemoryCache _cache;
    
    public async Task<bool> CheckRateLimitAsync(string key, int limit, TimeSpan window)
    {
        var cacheKey = $"rate_limit:{key}";
        var count = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = window;
            return 0;
        });
        
        if (count >= limit)
            return false;
            
        _cache.Set(cacheKey, count + 1, window);
        return true;
    }
}

// Usage in middleware
public class RateLimitingMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        var endpoint = $"{context.Request.Method}:{context.Request.Path}";
        var key = $"{ipAddress}:{endpoint}";
        
        if (!await _rateLimitingService.CheckRateLimitAsync(key, 100, TimeSpan.FromMinutes(1)))
        {
            context.Response.StatusCode = 429; // Too Many Requests
            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }
        
        await _next(context);
    }
}
```

---

## Security Testing Examples

### Security-Focused Unit Tests

```csharp
[TestClass]
public class AuthenticationSecurityTests
{
    [TestMethod]
    public async Task Login_ShouldLockAccount_AfterFailedAttempts()
    {
        // Arrange
        var authService = CreateAuthService();
        var request = new LoginRequest { Email = "test@example.com", Password = "wrong" };
        
        // Act & Assert
        for (int i = 0; i < 5; i++)
        {
            await Assert.ThrowsExceptionAsync<UnauthorizedException>(
                () => authService.LoginAsync(request));
        }
        
        // Should be locked now
        await Assert.ThrowsExceptionAsync<AccountLockedException>(
            () => authService.LoginAsync(request));
    }
    
    [TestMethod]
    public async Task Password_ShouldRejectCommonPasswords()
    {
        // Arrange
        var validator = new RegisterRequestValidator();
        var commonPasswords = new[] { "password", "12345678", "qwerty" };
        
        // Act & Assert
        foreach (var password in commonPasswords)
        {
            var request = new RegisterRequest { Password = password };
            var result = validator.Validate(request);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.PropertyName == "Password"));
        }
    }
}
```

---

*This implementation guide should be used in conjunction with the security checklist and policies to ensure comprehensive security coverage.*