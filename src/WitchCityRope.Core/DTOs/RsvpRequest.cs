using System;

namespace WitchCityRope.Core.DTOs
{
    /// <summary>
    /// Request model for creating an RSVP
    /// </summary>
    public class RsvpRequest
    {
        /// <summary>
        /// RSVP status: "Attending", "Maybe", "Not Attending"
        /// </summary>
        public string Status { get; set; } = "Attending";

        /// <summary>
        /// Optional comment or note
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Number of guests (if event allows +1s)
        /// </summary>
        public int? GuestCount { get; set; }

        /// <summary>
        /// Dietary restrictions for the attendee(s)
        /// </summary>
        public string? DietaryRestrictions { get; set; }

        /// <summary>
        /// Any accessibility needs
        /// </summary>
        public string? AccessibilityNeeds { get; set; }
    }
}