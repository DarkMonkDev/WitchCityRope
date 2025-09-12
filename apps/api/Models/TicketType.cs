using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Models;

/// <summary>
/// TicketType entity representing different ticket options for events/sessions
/// Supports single-session tickets, multi-session packages, and sliding scale pricing
/// </summary>
public class TicketType
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to the parent event
    /// </summary>
    [Required]
    public Guid EventId { get; set; }

    /// <summary>
    /// Reference to specific session (null for multi-session tickets)
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Ticket type name (e.g., "Early Bird", "Regular", "Day 1", "Full Event")
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this ticket includes
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Minimum price for sliding scale (what you can afford)
    /// </summary>
    [Required]
    public decimal Price { get; set; }

    /// <summary>
    /// Total number of tickets available
    /// </summary>
    [Required]
    public int Available { get; set; }

    /// <summary>
    /// Number of tickets sold
    /// </summary>
    [Required]
    public int Sold { get; set; } = 0;

    /// <summary>
    /// Whether this is an RSVP-mode ticket (free for Social events)
    /// </summary>
    public bool IsRsvpMode { get; set; } = false;

    /// <summary>
    /// Navigation property to parent event
    /// </summary>
    public Event? Event { get; set; }

    /// <summary>
    /// Navigation property to specific session (if single-session ticket)
    /// </summary>
    public Session? Session { get; set; }

    /// <summary>
    /// Navigation property to ticket purchases
    /// </summary>
    public ICollection<TicketPurchase> Purchases { get; set; } = new List<TicketPurchase>();

    /// <summary>
    /// When record was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When record was last updated
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets remaining available tickets
    /// </summary>
    public int Remaining => Available - Sold;

    /// <summary>
    /// Gets whether this ticket type is sold out
    /// </summary>
    public bool IsSoldOut => Sold >= Available;
}