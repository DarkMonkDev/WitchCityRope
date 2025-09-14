using System;
using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Events.Models
{
    /// <summary>
    /// Request model for updating an existing event session
    /// </summary>
    public class UpdateEventSessionRequest
    {
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