using System;

namespace WitchCityRope.Api.Features.Events.Models
{
    /// <summary>
    /// Data transfer object for Event Session
    /// </summary>
    public class EventSessionDto
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public string SessionIdentifier { get; set; } = null!;
        public string Name { get; set; } = null!;
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public int Capacity { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}