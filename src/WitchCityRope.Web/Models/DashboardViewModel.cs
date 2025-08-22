using System;
using System.Collections.Generic;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Web.Models
{
    public class DashboardViewModel
    {
        public string SceneName { get; set; } = string.Empty;
        public WitchCityRope.Core.Enums.UserRole Role { get; set; }
        public WitchCityRope.Core.Entities.VettingStatus VettingStatus { get; set; }
        public List<EventViewModel> UpcomingEvents { get; set; } = new();
        public MembershipStatsViewModel Stats { get; set; } = new();
    }

    public class EventViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public WitchCityRope.Core.Enums.EventType EventType { get; set; }
        public string InstructorName { get; set; } = string.Empty;
        public string RegistrationStatus { get; set; } = string.Empty;
        public Guid TicketId { get; set; }
    }

    public class MembershipStatsViewModel
    {
        public bool IsVerified { get; set; }
        public int EventsAttended { get; set; }
        public int MonthsAsMember { get; set; }
        public int ConsecutiveEvents { get; set; }
        public DateTime JoinDate { get; set; }
        public WitchCityRope.Core.Entities.VettingStatus VettingStatus { get; set; }
        public DateTime? NextInterviewDate { get; set; }
    }

    // Removed local enums - using Core entities instead
}