using System;
using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Events.Models
{
    /// <summary>
    /// Request model for creating a new event session
    /// </summary>
    public class CreateEventSessionRequest
    {
        /// <summary>
        /// Session identifier (e.g., S1, S2, S3)
        /// </summary>
        [Required]
        [StringLength(10, MinimumLength = 1)]
        public string SessionIdentifier { get; set; } = null!;

        /// <summary>
        /// Display name of the session
        /// </summary>
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Session start time (will be converted to UTC)
        /// </summary>
        [Required]
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// Session end time (will be converted to UTC)
        /// </summary>
        [Required]
        public DateTime EndDateTime { get; set; }

        /// <summary>
        /// Maximum capacity for this session
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Capacity must be greater than 0")]
        public int Capacity { get; set; }
    }
}