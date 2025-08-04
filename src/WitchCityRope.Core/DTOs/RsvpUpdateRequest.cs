using System;

namespace WitchCityRope.Core.DTOs
{
    /// <summary>
    /// Request model for updating an RSVP
    /// </summary>
    public class RsvpUpdateRequest
    {
        /// <summary>
        /// Updated RSVP status
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Updated comment
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Updated guest count
        /// </summary>
        public int? GuestCount { get; set; }

        /// <summary>
        /// Updated dietary restrictions
        /// </summary>
        public string? DietaryRestrictions { get; set; }

        /// <summary>
        /// Updated accessibility needs
        /// </summary>
        public string? AccessibilityNeeds { get; set; }
    }
}