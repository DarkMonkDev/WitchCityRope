using System;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Core.Entities;

/// <summary>
/// Represents an RSVP for a social event (meetup, rope jam, etc.)
/// RSVPs are free and do not require payment, unlike Tickets which are for paid events.
/// </summary>
public class Rsvp : BaseEntity
{
    // Private constructor for EF Core
    private Rsvp() 
    { 
        ConfirmationCode = string.Empty;
        Notes = string.Empty;
        Event = null!;
    }

    public Rsvp(Guid userId, Event @event)
    {
        if (@event == null) throw new ArgumentNullException(nameof(@event));
        
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        UserId = userId;
        EventId = @event.Id;
        Event = @event;
        RsvpDate = DateTime.UtcNow;
        Status = RsvpStatus.Confirmed;
        Notes = string.Empty;
        
        // Generate a unique confirmation code
        ConfirmationCode = GenerateConfirmationCode();
    }

    public Guid UserId { get; private set; }
    public Guid EventId { get; private set; }
    public DateTime RsvpDate { get; private set; }
    public RsvpStatus Status { get; private set; }
    public string ConfirmationCode { get; private set; }
    public DateTime? CheckedInAt { get; private set; }
    public Guid? CheckedInBy { get; private set; }
    public string Notes { get; private set; }

    // Navigation properties
    public virtual Event Event { get; private set; }

    // Business methods
    public void Cancel()
    {
        if (Status == RsvpStatus.Cancelled)
            throw new InvalidOperationException("RSVP is already cancelled.");
        
        if (Status == RsvpStatus.CheckedIn)
            throw new InvalidOperationException("Cannot cancel an RSVP after check-in.");
        
        Status = RsvpStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void CheckIn(Guid checkedInBy)
    {
        if (Status == RsvpStatus.Cancelled)
            throw new InvalidOperationException("Cannot check in a cancelled RSVP.");
        
        if (Status == RsvpStatus.CheckedIn)
            throw new InvalidOperationException("RSVP is already checked in.");
        
        if (Event.StartDate > DateTime.UtcNow.AddHours(2))
            throw new InvalidOperationException("Cannot check in before event starts.");
        
        CheckedInAt = DateTime.UtcNow;
        CheckedInBy = checkedInBy;
        Status = RsvpStatus.CheckedIn;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateNotes(string notes)
    {
        if (Status == RsvpStatus.Cancelled)
            throw new InvalidOperationException("Cannot update notes for a cancelled RSVP.");
            
        Notes = notes ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string GenerateConfirmationCode()
    {
        return $"RSVP-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
    }
}