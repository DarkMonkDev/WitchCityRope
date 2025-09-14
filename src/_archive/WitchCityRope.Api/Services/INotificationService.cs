namespace WitchCityRope.Api.Services;

public interface INotificationService
{
    Task SendEventRegistrationConfirmationAsync(Guid userId, Guid eventId);
    Task SendEventCancellationNotificationAsync(Guid eventId);
    Task SendEventReminderAsync(Guid eventId);
    Task SendVettingStatusUpdateAsync(Guid userId, bool approved);
    Task SendPasswordResetAsync(string email, string resetToken);
    Task SendEmailVerificationAsync(string email, string verificationToken);
}