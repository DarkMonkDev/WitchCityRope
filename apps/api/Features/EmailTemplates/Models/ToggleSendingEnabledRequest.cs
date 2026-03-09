using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.EmailTemplates.Models;

/// <summary>
/// Request model for toggling the SendingEnabled flag on a global template.
/// Used by PATCH /api/email-templates/{id}/sending-enabled endpoint.
/// Works for all categories (Vetting, Events, Admin, Incident).
/// </summary>
public class ToggleSendingEnabledRequest
{
    [Required(ErrorMessage = "Enabled flag is required")]
    public bool Enabled { get; set; }
}
