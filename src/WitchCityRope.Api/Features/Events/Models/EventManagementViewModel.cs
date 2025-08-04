using System;

namespace WitchCityRope.Api.Features.Events
{
    /// <summary>
    /// View model for event management dashboard
    /// </summary>
    public class EventManagementViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int RegisteredCount { get; set; }
        public decimal Price { get; set; }
        public decimal TotalRevenue { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int RefundCutoffHours { get; set; } = 48;
        public DateTime? RegistrationOpensAt { get; set; }
        public DateTime? RegistrationClosesAt { get; set; }
        public WitchCityRope.Core.Enums.TicketType TicketTypes { get; set; } = WitchCityRope.Core.Enums.TicketType.Individual;
        public decimal? IndividualPrice { get; set; }
        public decimal? CouplesPrice { get; set; }
    }
}