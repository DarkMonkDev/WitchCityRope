using System;
using System.Threading.Tasks;
using WitchCityRope.Api.Services;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Auth.TwoFactor;

/// <summary>
/// Command to enable two-factor authentication for a user account
/// Generates TOTP secret and QR code for authenticator apps
/// </summary>
public record EnableTwoFactorCommand(
    Guid UserId,
    string Password // Require password confirmation for security
) : IRequest<EnableTwoFactorResult>;

public record EnableTwoFactorResult(
    string Secret,
    string QrCodeUri,
    string ManualEntryKey,
    string[] BackupCodes
);

/// <summary>
/// Handles enabling two-factor authentication
/// Generates TOTP secret, backup codes, and QR code for authenticator setup
/// </summary>
public class EnableTwoFactorCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITwoFactorService _twoFactorService;

    public EnableTwoFactorCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITwoFactorService twoFactorService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _twoFactorService = twoFactorService;
    }

    public async Task<EnableTwoFactorResult> Execute(EnableTwoFactorCommand command)
    {
        // Get user and verify they exist
        var user = await _userRepository.GetByIdAsync(command.UserId);
        if (user == null)
        {
            throw new NotFoundException("User not found");
        }

        // Verify password for security
        if (!_passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid password");
        }

        // Check if 2FA is already enabled
        if (user.TwoFactorEnabled)
        {
            throw new ConflictException("Two-factor authentication is already enabled");
        }

        // Generate TOTP secret
        var secret = _twoFactorService.GenerateSecret();
        
        // Generate QR code URI for authenticator apps
        var qrCodeUri = _twoFactorService.GenerateQrCodeUri(
            issuer: "WitchCityRope",
            accountName: user.Email,
            secret: secret
        );

        // Generate backup codes
        var backupCodes = GenerateBackupCodes(8);

        // Save the secret and backup codes (encrypted)
        user.TwoFactorSecret = _twoFactorService.EncryptSecret(secret);
        user.TwoFactorBackupCodes = backupCodes.Select(code => 
            _passwordHasher.HashPassword(code)).ToList();
        user.TwoFactorEnabled = false; // Will be enabled after verification

        await _userRepository.UpdateAsync(user);

        return new EnableTwoFactorResult(
            Secret: secret,
            QrCodeUri: qrCodeUri,
            ManualEntryKey: FormatManualEntryKey(secret),
            BackupCodes: backupCodes.ToArray()
        );
    }

    private List<string> GenerateBackupCodes(int count)
    {
        var codes = new List<string>();
        var random = new Random();
        
        for (int i = 0; i < count; i++)
        {
            // Generate 8-character alphanumeric codes
            var code = string.Empty;
            for (int j = 0; j < 8; j++)
            {
                var isDigit = random.Next(2) == 0;
                if (isDigit)
                {
                    code += random.Next(10).ToString();
                }
                else
                {
                    code += (char)('A' + random.Next(26));
                }
            }
            codes.Add(code);
        }
        
        return codes;
    }

    private string FormatManualEntryKey(string secret)
    {
        // Format secret for manual entry (groups of 4 characters)
        var formatted = string.Empty;
        for (int i = 0; i < secret.Length; i += 4)
        {
            if (i > 0) formatted += " ";
            formatted += secret.Substring(i, Math.Min(4, secret.Length - i));
        }
        return formatted;
    }
}

/// <summary>
/// Command to verify and complete two-factor setup
/// </summary>
public record VerifyTwoFactorCommand(
    Guid UserId,
    string VerificationCode
) : IRequest<VerifyTwoFactorResult>;

public record VerifyTwoFactorResult(
    bool Success,
    string Message
);

/// <summary>
/// Verifies the TOTP code to complete 2FA setup
/// </summary>
public class VerifyTwoFactorCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ITwoFactorService _twoFactorService;

    public VerifyTwoFactorCommandHandler(
        IUserRepository userRepository,
        ITwoFactorService twoFactorService)
    {
        _userRepository = userRepository;
        _twoFactorService = twoFactorService;
    }

    public async Task<VerifyTwoFactorResult> Execute(VerifyTwoFactorCommand command)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId);
        if (user == null)
        {
            throw new NotFoundException("User not found");
        }

        // Verify the code
        var secret = _twoFactorService.DecryptSecret(user.TwoFactorSecret!);
        var isValid = _twoFactorService.VerifyCode(secret, command.VerificationCode);

        if (!isValid)
        {
            return new VerifyTwoFactorResult(
                Success: false,
                Message: "Invalid verification code"
            );
        }

        // Enable 2FA
        user.TwoFactorEnabled = true;
        user.TwoFactorEnabledAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        return new VerifyTwoFactorResult(
            Success: true,
            Message: "Two-factor authentication has been enabled successfully"
        );
    }
}

// Marker interface for future MediatR implementation
public interface IRequest<TResponse> { }

// Custom exceptions
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

// Service interfaces
public interface ITwoFactorService
{
    string GenerateSecret();
    string GenerateQrCodeUri(string issuer, string accountName, string secret);
    bool VerifyCode(string secret, string code);
    string EncryptSecret(string secret);
    string DecryptSecret(string encryptedSecret);
}

// Extended User model for 2FA
public partial class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }
    public List<string> TwoFactorBackupCodes { get; set; } = new();
    public DateTime? TwoFactorEnabledAt { get; set; }
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId);
    Task UpdateAsync(User user);
}