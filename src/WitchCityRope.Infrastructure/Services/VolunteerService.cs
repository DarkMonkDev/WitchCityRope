using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Interfaces;
using WitchCityRope.Infrastructure.Data;

namespace WitchCityRope.Infrastructure.Services
{
    public class VolunteerService : IVolunteerService
    {
        private readonly WitchCityRopeIdentityDbContext _context;
        private readonly ILogger<VolunteerService> _logger;

        public VolunteerService(
            WitchCityRopeIdentityDbContext context,
            ILogger<VolunteerService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Volunteer Task Management

        public async Task<List<VolunteerTask>> GetEventVolunteerTasksAsync(Guid eventId)
        {
            return await _context.VolunteerTasks
                .Include(t => t.Assignments)
                .Where(t => t.EventId == eventId)
                .OrderBy(t => t.StartTime)
                .ToListAsync();
        }

        public async Task<VolunteerTask> GetVolunteerTaskByIdAsync(Guid taskId)
        {
            return await _context.VolunteerTasks
                .Include(t => t.Assignments)
                .Include(t => t.Event)
                .FirstOrDefaultAsync(t => t.Id == taskId);
        }

        public async Task<VolunteerTask> CreateVolunteerTaskAsync(VolunteerTask task)
        {
            _context.VolunteerTasks.Add(task);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created volunteer task {TaskId} for event {EventId}", task.Id, task.EventId);
            
            return task;
        }

        public async Task<VolunteerTask> UpdateVolunteerTaskAsync(VolunteerTask task)
        {
            _context.VolunteerTasks.Update(task);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Updated volunteer task {TaskId}", task.Id);
            
            return task;
        }

        public async Task DeleteVolunteerTaskAsync(Guid taskId)
        {
            var task = await _context.VolunteerTasks.FindAsync(taskId);
            if (task != null)
            {
                _context.VolunteerTasks.Remove(task);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deleted volunteer task {TaskId}", taskId);
            }
        }

        // Volunteer Assignment Management

        public async Task<List<VolunteerAssignment>> GetTaskAssignmentsAsync(Guid taskId)
        {
            return await _context.VolunteerAssignments
                .Include(a => a.Task)
                .Where(a => a.TaskId == taskId)
                .ToListAsync();
        }

        public async Task<List<VolunteerAssignment>> GetUserVolunteerAssignmentsAsync(Guid userId)
        {
            return await _context.VolunteerAssignments
                .Include(a => a.Task)
                    .ThenInclude(t => t.Event)
                .Where(a => a.UserId == userId)
                .OrderBy(a => a.Task.StartTime)
                .ToListAsync();
        }

        public async Task<VolunteerAssignment> GetAssignmentByIdAsync(Guid assignmentId)
        {
            return await _context.VolunteerAssignments
                .Include(a => a.Task)
                    .ThenInclude(t => t.Event)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);
        }

        public async Task<VolunteerAssignment> CreateAssignmentAsync(VolunteerAssignment assignment)
        {
            _context.VolunteerAssignments.Add(assignment);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created volunteer assignment {AssignmentId} for user {UserId} on task {TaskId}", 
                assignment.Id, assignment.UserId, assignment.TaskId);
            
            return assignment;
        }

        public async Task<VolunteerAssignment> UpdateAssignmentAsync(VolunteerAssignment assignment)
        {
            _context.VolunteerAssignments.Update(assignment);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Updated volunteer assignment {AssignmentId}", assignment.Id);
            
            return assignment;
        }

        public async Task DeleteAssignmentAsync(Guid assignmentId)
        {
            var assignment = await _context.VolunteerAssignments.FindAsync(assignmentId);
            if (assignment != null)
            {
                _context.VolunteerAssignments.Remove(assignment);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deleted volunteer assignment {AssignmentId}", assignmentId);
            }
        }

        // Volunteer Operations

        public async Task<bool> AssignVolunteerToTaskAsync(Guid taskId, Guid userId)
        {
            try
            {
                var task = await GetVolunteerTaskByIdAsync(taskId);
                if (task == null)
                {
                    _logger.LogWarning("Task {TaskId} not found", taskId);
                    return false;
                }

                // Check if user is already assigned
                var existingAssignment = await _context.VolunteerAssignments
                    .FirstOrDefaultAsync(a => a.TaskId == taskId && a.UserId == userId);
                
                if (existingAssignment != null)
                {
                    _logger.LogWarning("User {UserId} is already assigned to task {TaskId}", userId, taskId);
                    return false;
                }

                // Check if task has available slots
                var availableSlots = await GetAvailableSlotsForTaskAsync(taskId);
                if (availableSlots <= 0)
                {
                    _logger.LogWarning("No available slots for task {TaskId}", taskId);
                    return false;
                }

                var assignment = new VolunteerAssignment(taskId, userId);
                await CreateAssignmentAsync(assignment);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning volunteer {UserId} to task {TaskId}", userId, taskId);
                return false;
            }
        }

        public async Task<bool> UnassignVolunteerFromTaskAsync(Guid taskId, Guid userId)
        {
            try
            {
                var assignment = await _context.VolunteerAssignments
                    .FirstOrDefaultAsync(a => a.TaskId == taskId && a.UserId == userId);
                
                if (assignment == null)
                {
                    _logger.LogWarning("Assignment not found for user {UserId} on task {TaskId}", userId, taskId);
                    return false;
                }

                await DeleteAssignmentAsync(assignment.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unassigning volunteer {UserId} from task {TaskId}", userId, taskId);
                return false;
            }
        }

        public async Task<bool> ConfirmVolunteerAssignmentAsync(Guid assignmentId)
        {
            try
            {
                var assignment = await GetAssignmentByIdAsync(assignmentId);
                if (assignment == null)
                {
                    _logger.LogWarning("Assignment {AssignmentId} not found", assignmentId);
                    return false;
                }

                assignment.Confirm();
                await UpdateAssignmentAsync(assignment);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming volunteer assignment {AssignmentId}", assignmentId);
                return false;
            }
        }

        public async Task<bool> CancelVolunteerAssignmentAsync(Guid assignmentId)
        {
            try
            {
                var assignment = await GetAssignmentByIdAsync(assignmentId);
                if (assignment == null)
                {
                    _logger.LogWarning("Assignment {AssignmentId} not found", assignmentId);
                    return false;
                }

                assignment.Cancel();
                await UpdateAssignmentAsync(assignment);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling volunteer assignment {AssignmentId}", assignmentId);
                return false;
            }
        }

        // Volunteer Ticket Management

        public async Task<bool> MarkVolunteerTicketUsedAsync(Guid assignmentId)
        {
            try
            {
                var assignment = await GetAssignmentByIdAsync(assignmentId);
                if (assignment == null)
                {
                    _logger.LogWarning("Assignment {AssignmentId} not found", assignmentId);
                    return false;
                }

                assignment.UpdateTicketStatus(true, assignment.TicketPrice);
                await UpdateAssignmentAsync(assignment);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking volunteer ticket as used for assignment {AssignmentId}", assignmentId);
                return false;
            }
        }

        public async Task<bool> UpdateBackgroundCheckStatusAsync(Guid assignmentId, bool isApproved)
        {
            try
            {
                var assignment = await GetAssignmentByIdAsync(assignmentId);
                if (assignment == null)
                {
                    _logger.LogWarning("Assignment {AssignmentId} not found", assignmentId);
                    return false;
                }

                assignment.UpdateBackgroundCheckStatus(isApproved);
                
                await UpdateAssignmentAsync(assignment);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating background check status for assignment {AssignmentId}", assignmentId);
                return false;
            }
        }

        // Query Methods

        public async Task<int> GetAvailableSlotsForTaskAsync(Guid taskId)
        {
            var task = await GetVolunteerTaskByIdAsync(taskId);
            if (task == null)
                return 0;

            var assignedCount = await _context.VolunteerAssignments
                .Where(a => a.TaskId == taskId && a.Status != VolunteerStatus.Cancelled)
                .CountAsync();

            return Math.Max(0, task.RequiredVolunteers - assignedCount);
        }

        public async Task<bool> IsUserAssignedToEventAsync(Guid eventId, Guid userId)
        {
            return await _context.VolunteerAssignments
                .Include(a => a.Task)
                .AnyAsync(a => a.Task.EventId == eventId && a.UserId == userId && a.Status != VolunteerStatus.Cancelled);
        }

        public async Task<List<VolunteerTask>> GetTasksNeedingVolunteersAsync(Guid eventId)
        {
            var tasks = await GetEventVolunteerTasksAsync(eventId);
            var tasksNeedingVolunteers = new List<VolunteerTask>();

            foreach (var task in tasks)
            {
                var availableSlots = await GetAvailableSlotsForTaskAsync(task.Id);
                if (availableSlots > 0)
                {
                    tasksNeedingVolunteers.Add(task);
                }
            }

            return tasksNeedingVolunteers;
        }
    }
}