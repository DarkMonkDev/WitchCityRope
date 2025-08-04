using System;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Core.DTOs;

/// <summary>
/// DTO for user's event registration information
/// </summary>
public class UserRegistrationDto
{
    public Guid RegistrationId { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string EventLocation { get; set; } = string.Empty;
    public DateTime EventStartDate { get; set; }
    public DateTime EventEndDate { get; set; }
    public EventType EventType { get; set; }
    public TicketStatus Status { get; set; }
    public DateTime RegisteredAt { get; set; }
    public decimal AmountPaid { get; set; }
    public string ConfirmationCode { get; set; } = string.Empty;
    public int? WaitlistPosition { get; set; }
    public bool CanCancel { get; set; }
}