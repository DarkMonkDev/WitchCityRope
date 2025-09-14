using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Events.Models
{
    /// <summary>
    /// Request model for updating an existing event ticket type
    /// </summary>
    public class UpdateEventTicketTypeRequest
    {
        /// <summary>
        /// Display name of the ticket type
        /// </summary>
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Minimum price for sliding scale pricing
        /// </summary>
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Minimum price cannot be negative")]
        public decimal MinPrice { get; set; }

        /// <summary>
        /// Maximum price for sliding scale pricing
        /// </summary>
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Maximum price cannot be negative")]
        public decimal MaxPrice { get; set; }

        /// <summary>
        /// Available quantity (null for unlimited)
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0 if specified")]
        public int? QuantityAvailable { get; set; }

        /// <summary>
        /// End of sales period (null for no limit)
        /// </summary>
        public DateTime? SalesEndDateTime { get; set; }

        /// <summary>
        /// Session identifiers to include in this ticket type
        /// </summary>
        [Required]
        [MinLength(1, ErrorMessage = "At least one session must be included")]
        public ICollection<string> IncludedSessionIdentifiers { get; set; } = new List<string>();
    }
}