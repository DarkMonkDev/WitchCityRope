using WitchCityRope.Core.Enums;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Web.Models
{
    // DTOs for Dashboard API responses
    public class DashboardDto
    {
        public string SceneName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public VettingStatus VettingStatus { get; set; }
    }
    
    public class EventDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public EventType EventType { get; set; }
        public string InstructorName { get; set; } = string.Empty;
        public string RegistrationStatus { get; set; } = string.Empty;
        public Guid TicketId { get; set; }
    }
    
    public class MembershipStatsDto
    {
        public bool IsVerified { get; set; }
        public int EventsAttended { get; set; }
        public int MonthsAsMember { get; set; }
        public int ConsecutiveEvents { get; set; }
        public DateTime JoinDate { get; set; }
        public VettingStatus VettingStatus { get; set; }
        public DateTime? NextInterviewDate { get; set; }
    }
}