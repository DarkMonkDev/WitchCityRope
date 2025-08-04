using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Interfaces;

namespace WitchCityRope.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class VolunteerController : ControllerBase
    {
        private readonly IVolunteerService _volunteerService;
        private readonly ILogger<VolunteerController> _logger;

        public VolunteerController(
            IVolunteerService volunteerService,
            ILogger<VolunteerController> logger)
        {
            _volunteerService = volunteerService;
            _logger = logger;
        }

        // Volunteer Task Management
        
        [HttpGet("events/{eventId}/tasks")]
        [ProducesResponseType(typeof(List<VolunteerTaskDto>), 200)]
        public async Task<ActionResult<List<VolunteerTaskDto>>> GetEventVolunteerTasks(Guid eventId)
        {
            try
            {
                var tasks = await _volunteerService.GetEventVolunteerTasksAsync(eventId);
                
                // Map to DTOs
                var taskDtos = tasks.Select(t => new VolunteerTaskDto
                {
                    Id = t.Id,
                    EventId = t.EventId,
                    Name = t.Name,
                    Description = t.Description,
                    StartTime = t.StartTime,
                    EndTime = t.EndTime,
                    RequiredVolunteers = t.RequiredVolunteers,
                    Assignments = t.Assignments.Select(a => new VolunteerAssignmentDto
                    {
                        Id = a.Id,
                        TaskId = a.TaskId,
                        UserId = a.UserId,
                        UserSceneName = "Unknown", // TODO: Fetch user scene name separately
                        Status = a.Status,
                        HasTicket = a.HasTicket,
                        BackgroundCheckVerified = a.BackgroundCheckVerified,
                        AssignedAt = a.AssignedAt
                    }).ToList()
                }).ToList();
                
                return Ok(taskDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting volunteer tasks for event {EventId}", eventId);
                return StatusCode(500, "An error occurred while retrieving volunteer tasks");
            }
        }

        [HttpGet("tasks/{taskId}")]
        [ProducesResponseType(typeof(VolunteerTaskDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<VolunteerTaskDto>> GetVolunteerTask(Guid taskId)
        {
            try
            {
                var task = await _volunteerService.GetVolunteerTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound();
                
                var taskDto = new VolunteerTaskDto
                {
                    Id = task.Id,
                    EventId = task.EventId,
                    Name = task.Name,
                    Description = task.Description,
                    StartTime = task.StartTime,
                    EndTime = task.EndTime,
                    RequiredVolunteers = task.RequiredVolunteers,
                    Assignments = task.Assignments.Select(a => new VolunteerAssignmentDto
                    {
                        Id = a.Id,
                        TaskId = a.TaskId,
                        UserId = a.UserId,
                        UserSceneName = "Unknown", // TODO: Fetch user scene name separately
                        Status = a.Status,
                        HasTicket = a.HasTicket,
                        BackgroundCheckVerified = a.BackgroundCheckVerified,
                        AssignedAt = a.AssignedAt
                    }).ToList()
                };
                
                return Ok(taskDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting volunteer task {TaskId}", taskId);
                return StatusCode(500, "An error occurred while retrieving the volunteer task");
            }
        }

        [HttpPost("events/{eventId}/tasks")]
        [Authorize(Roles = "Administrator,Organizer")]
        [ProducesResponseType(typeof(VolunteerTaskDto), 201)]
        public async Task<ActionResult<VolunteerTaskDto>> CreateVolunteerTask(Guid eventId, [FromBody] CreateVolunteerTaskRequest request)
        {
            try
            {
                var task = new VolunteerTask(
                    eventId: eventId,
                    name: request.Name,
                    description: request.Description,
                    startTime: request.StartTime,
                    endTime: request.EndTime,
                    requiredVolunteers: request.RequiredVolunteers);
                
                var createdTask = await _volunteerService.CreateVolunteerTaskAsync(task);
                
                var taskDto = new VolunteerTaskDto
                {
                    Id = createdTask.Id,
                    EventId = createdTask.EventId,
                    Name = createdTask.Name,
                    Description = createdTask.Description,
                    StartTime = createdTask.StartTime,
                    EndTime = createdTask.EndTime,
                    RequiredVolunteers = createdTask.RequiredVolunteers,
                    Assignments = new List<VolunteerAssignmentDto>()
                };
                
                return CreatedAtAction(nameof(GetVolunteerTask), new { taskId = taskDto.Id }, taskDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating volunteer task for event {EventId}", eventId);
                return StatusCode(500, "An error occurred while creating the volunteer task");
            }
        }

        [HttpPut("tasks/{taskId}")]
        [Authorize(Roles = "Administrator,Organizer")]
        [ProducesResponseType(typeof(VolunteerTaskDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<VolunteerTaskDto>> UpdateVolunteerTask(Guid taskId, [FromBody] UpdateVolunteerTaskRequest request)
        {
            try
            {
                var task = await _volunteerService.GetVolunteerTaskByIdAsync(taskId);
                if (task == null)
                    return NotFound();
                
                task.UpdateDetails(request.Name, request.Description, request.StartTime, request.EndTime, request.RequiredVolunteers);
                
                var updatedTask = await _volunteerService.UpdateVolunteerTaskAsync(task);
                
                var taskDto = new VolunteerTaskDto
                {
                    Id = updatedTask.Id,
                    EventId = updatedTask.EventId,
                    Name = updatedTask.Name,
                    Description = updatedTask.Description,
                    StartTime = updatedTask.StartTime,
                    EndTime = updatedTask.EndTime,
                    RequiredVolunteers = updatedTask.RequiredVolunteers,
                    Assignments = updatedTask.Assignments.Select(a => new VolunteerAssignmentDto
                    {
                        Id = a.Id,
                        TaskId = a.TaskId,
                        UserId = a.UserId,
                        UserSceneName = "Unknown", // TODO: Fetch user scene name separately
                        Status = a.Status,
                        HasTicket = a.HasTicket,
                        BackgroundCheckVerified = a.BackgroundCheckVerified,
                        AssignedAt = a.AssignedAt
                    }).ToList()
                };
                
                return Ok(taskDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating volunteer task {TaskId}", taskId);
                return StatusCode(500, "An error occurred while updating the volunteer task");
            }
        }

        [HttpDelete("tasks/{taskId}")]
        [Authorize(Roles = "Administrator,Organizer")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteVolunteerTask(Guid taskId)
        {
            try
            {
                await _volunteerService.DeleteVolunteerTaskAsync(taskId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting volunteer task {TaskId}", taskId);
                return StatusCode(500, "An error occurred while deleting the volunteer task");
            }
        }

        // Volunteer Assignment Management

        [HttpPost("tasks/{taskId}/assignments")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<ActionResult<bool>> AssignVolunteer(Guid taskId, [FromBody] AssignVolunteerRequest request)
        {
            try
            {
                var result = await _volunteerService.AssignVolunteerToTaskAsync(taskId, request.UserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning volunteer to task {TaskId}", taskId);
                return StatusCode(500, "An error occurred while assigning the volunteer");
            }
        }

        [HttpDelete("tasks/{taskId}/assignments/{userId}")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<ActionResult<bool>> UnassignVolunteer(Guid taskId, Guid userId)
        {
            try
            {
                var result = await _volunteerService.UnassignVolunteerFromTaskAsync(taskId, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unassigning volunteer from task {TaskId}", taskId);
                return StatusCode(500, "An error occurred while unassigning the volunteer");
            }
        }

        [HttpPost("assignments/{assignmentId}/confirm")]
        [Authorize(Roles = "Administrator,Organizer")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<ActionResult<bool>> ConfirmAssignment(Guid assignmentId)
        {
            try
            {
                var result = await _volunteerService.ConfirmVolunteerAssignmentAsync(assignmentId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming volunteer assignment {AssignmentId}", assignmentId);
                return StatusCode(500, "An error occurred while confirming the assignment");
            }
        }

        [HttpPost("assignments/{assignmentId}/ticket")]
        [Authorize(Roles = "Administrator,Organizer")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<ActionResult<bool>> MarkTicketUsed(Guid assignmentId)
        {
            try
            {
                var result = await _volunteerService.MarkVolunteerTicketUsedAsync(assignmentId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking volunteer ticket as used for assignment {AssignmentId}", assignmentId);
                return StatusCode(500, "An error occurred while marking the ticket as used");
            }
        }

        [HttpGet("users/{userId}/assignments")]
        [ProducesResponseType(typeof(List<VolunteerAssignmentDto>), 200)]
        public async Task<ActionResult<List<VolunteerAssignmentDto>>> GetUserAssignments(Guid userId)
        {
            try
            {
                var assignments = await _volunteerService.GetUserVolunteerAssignmentsAsync(userId);
                
                var assignmentDtos = assignments.Select(a => new VolunteerAssignmentDto
                {
                    Id = a.Id,
                    TaskId = a.TaskId,
                    UserId = a.UserId,
                    UserSceneName = "Unknown", // TODO: Fetch user scene name separately
                    Status = a.Status,
                    HasTicket = a.HasTicket,
                    BackgroundCheckVerified = a.BackgroundCheckVerified,
                    AssignedAt = a.AssignedAt
                }).ToList();
                
                return Ok(assignmentDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting volunteer assignments for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving user assignments");
            }
        }
    }

    // Request DTOs
    public class AssignVolunteerRequest
    {
        public Guid UserId { get; set; }
    }
}