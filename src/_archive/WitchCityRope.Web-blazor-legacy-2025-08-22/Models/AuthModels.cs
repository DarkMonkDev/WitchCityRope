namespace WitchCityRope.Web.Models;

// Request/Response models for authentication
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? Error { get; set; }
    public bool RequiresTwoFactor { get; set; }
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SceneName { get; set; } = string.Empty;
}

public class RegisterResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

// Result models for IAuthService
public class LoginResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public bool RequiresTwoFactor { get; set; }
    public string? Token { get; set; }
    public UserDto? User { get; set; }
}

public class RegisterResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public bool RequiresEmailVerification { get; set; }
}

// Two-Factor Authentication models
public class TwoFactorVerifyRequest
{
    public string Code { get; set; } = string.Empty;
    public bool RememberDevice { get; set; }
}

public class TwoFactorVerifyResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Token { get; set; }
    public UserDto? User { get; set; }
}

// Password Reset models
public class PasswordResetRequest
{
    public string Email { get; set; } = string.Empty;
}

public class PasswordResetResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Message { get; set; }
}

public class PasswordResetConfirmRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class PasswordResetConfirmResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

// Two-Factor Setup models
public class TwoFactorSetupResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? QrCodeDataUri { get; set; }
    public string? SecretKey { get; set; }
    public string? AccountName { get; set; }
}

public class TwoFactorCompleteResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<string> BackupCodes { get; set; } = new();
}