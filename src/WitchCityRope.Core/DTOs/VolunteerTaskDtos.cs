using System;
using System.Collections.Generic;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Core.DTOs
{
    /// <summary>
    /// DTO for displaying volunteer task information
    /// </summary>
    public class VolunteerTaskDto
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int RequiredVolunteers { get; set; }
        public int ConfirmedVolunteers { get; set; }
        public List<VolunteerAssignmentDto> Assignments { get; set; } = new();
    }

    /// <summary>
    /// DTO for displaying volunteer assignment information
    /// </summary>
    public class VolunteerAssignmentDto
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserSceneName { get; set; } = string.Empty;
        public VolunteerStatus Status { get; set; }
        public bool HasTicket { get; set; }
        public decimal TicketPrice { get; set; }
        public bool BackgroundCheckVerified { get; set; }
        public DateTime AssignedAt { get; set; }
    }

    /// <summary>
    /// Request to create a new volunteer task
    /// </summary>
    public class CreateVolunteerTaskRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int RequiredVolunteers { get; set; } = 1;
    }

    /// <summary>
    /// Request to update a volunteer task
    /// </summary>
    public class UpdateVolunteerTaskRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int RequiredVolunteers { get; set; }
    }

    /// <summary>
    /// Request to assign a volunteer to a task
    /// </summary>
    public class AssignVolunteerRequest
    {
        public Guid UserId { get; set; }
    }

    /// <summary>
    /// Request to update volunteer ticket status
    /// </summary>
    public class UpdateVolunteerTicketRequest
    {
        public bool HasTicket { get; set; }
        public decimal TicketPrice { get; set; }
    }

    /// <summary>
    /// Summary of volunteer information for display
    /// </summary>
    public class VolunteerSummaryDto
    {
        public int TotalVolunteers { get; set; }
        public int ConfirmedVolunteers { get; set; }
        public int PendingVolunteers { get; set; }
        public int VolunteersWithTickets { get; set; }
        public int VolunteersNeedingTickets { get; set; }
    }
}