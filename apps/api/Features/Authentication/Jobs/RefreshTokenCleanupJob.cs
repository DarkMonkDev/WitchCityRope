using WitchCityRope.Api.Features.Authentication.Services;

namespace WitchCityRope.Api.Features.Authentication.Jobs;

/// <summary>
/// Hangfire scheduled job that cleans up expired and revoked refresh tokens.
/// Runs daily at 4 AM UTC to prevent database bloat from accumulated tokens.
/// Tokens are retained for 30 days after expiration for security audit purposes.
/// </summary>
public class RefreshTokenCleanupJob
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<RefreshTokenCleanupJob> _logger;

    public RefreshTokenCleanupJob(
        IRefreshTokenService refreshTokenService,
        ILogger<RefreshTokenCleanupJob> logger)
    {
        _refreshTokenService = refreshTokenService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting refresh token cleanup job");

        try
        {
            var deletedCount = await _refreshTokenService.CleanupExpiredTokensAsync(
                retentionDays: 30, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Refresh token cleanup completed. Deleted {Count} expired tokens", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh token cleanup job failed");
            throw; // Let Hangfire handle retry
        }
    }
}
