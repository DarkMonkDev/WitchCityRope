using System;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Core.DTOs;

/// <summary>
/// DTO for event attendee information
/// </summary>
public class EventAttendeeDto
{
    public Guid RegistrationId { get; set; }
    public Guid UserId { get; set; }
    public string SceneName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public DateTime RegisteredAt { get; set; }
    public decimal AmountPaid { get; set; }
    public string? DietaryRestrictions { get; set; }
    public string? AccessibilityNeeds { get; set; }
    public bool CheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public int? WaitlistPosition { get; set; }
}