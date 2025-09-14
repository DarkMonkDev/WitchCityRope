using System;
using System.Collections.Generic;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Api.Features.Events.Models
{
    /// <summary>
    /// Data transfer object for Event Ticket Type
    /// </summary>
    public class EventTicketTypeDto
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public string Name { get; set; } = null!;
        public TicketTypeEnum TicketType { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public int? QuantityAvailable { get; set; }
        public DateTime? SalesEndDateTime { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        /// <summary>
        /// Session identifiers included in this ticket type
        /// </summary>
        public ICollection<string> IncludedSessions { get; set; } = new List<string>();
        
        /// <summary>
        /// Available quantity (null for unlimited)
        /// </summary>
        public int? AvailableQuantity { get; set; }
        
        /// <summary>
        /// Whether sales are still open
        /// </summary>
        public bool SalesOpen { get; set; }
    }
}