using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Core.Interfaces
{
    public interface IVolunteerService
    {
        // Volunteer Task Management
        Task<List<VolunteerTask>> GetEventVolunteerTasksAsync(Guid eventId);
        Task<VolunteerTask> GetVolunteerTaskByIdAsync(Guid taskId);
        Task<VolunteerTask> CreateVolunteerTaskAsync(VolunteerTask task);
        Task<VolunteerTask> UpdateVolunteerTaskAsync(VolunteerTask task);
        Task DeleteVolunteerTaskAsync(Guid taskId);
        
        // Volunteer Assignment Management
        Task<List<VolunteerAssignment>> GetTaskAssignmentsAsync(Guid taskId);
        Task<List<VolunteerAssignment>> GetUserVolunteerAssignmentsAsync(Guid userId);
        Task<VolunteerAssignment> GetAssignmentByIdAsync(Guid assignmentId);
        Task<VolunteerAssignment> CreateAssignmentAsync(VolunteerAssignment assignment);
        Task<VolunteerAssignment> UpdateAssignmentAsync(VolunteerAssignment assignment);
        Task DeleteAssignmentAsync(Guid assignmentId);
        
        // Volunteer Operations
        Task<bool> AssignVolunteerToTaskAsync(Guid taskId, Guid userId);
        Task<bool> UnassignVolunteerFromTaskAsync(Guid taskId, Guid userId);
        Task<bool> ConfirmVolunteerAssignmentAsync(Guid assignmentId);
        Task<bool> CancelVolunteerAssignmentAsync(Guid assignmentId);
        
        // Volunteer Ticket Management
        Task<bool> MarkVolunteerTicketUsedAsync(Guid assignmentId);
        Task<bool> UpdateBackgroundCheckStatusAsync(Guid assignmentId, bool isApproved);
        
        // Query Methods
        Task<int> GetAvailableSlotsForTaskAsync(Guid taskId);
        Task<bool> IsUserAssignedToEventAsync(Guid eventId, Guid userId);
        Task<List<VolunteerTask>> GetTasksNeedingVolunteersAsync(Guid eventId);
    }
}