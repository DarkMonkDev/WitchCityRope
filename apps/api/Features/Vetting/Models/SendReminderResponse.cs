namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Response returned after sending an interview reminder email.
/// Includes updated reminder count and timestamp for display in the admin UI.
/// </summary>
public class SendReminderResponse
{
    public int RemindersSentCount { get; set; }
    public DateTime? LastReminderSentAt { get; set; }
}
