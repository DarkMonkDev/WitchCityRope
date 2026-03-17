using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Request model for sending interview reminder email to applicant.
/// Custom message is optional - the InterviewReminder email template provides default content.
/// The custom message value replaces the {{custom_message}} variable in the template.
/// </summary>
public class SendReminderRequest
{
    [StringLength(2000)]
    public string? CustomMessage { get; set; }
}
