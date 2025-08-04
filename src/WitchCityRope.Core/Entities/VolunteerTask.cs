using System;
using System.Collections.Generic;
using System.Linq;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Represents a volunteer task/position for an event
    /// </summary>
    public class VolunteerTask
    {
        private readonly List<VolunteerAssignment> _assignments = new();

        // Private constructor for EF Core
        private VolunteerTask()
        {
            Name = null!;
            Description = null!;
        }

        public VolunteerTask(
            Guid eventId,
            string name,
            string description,
            TimeOnly startTime,
            TimeOnly endTime,
            int requiredVolunteers = 1)
        {
            if (eventId == Guid.Empty)
                throw new ArgumentException("Event ID cannot be empty", nameof(eventId));
            
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Task name is required", nameof(name));
            
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Task description is required", nameof(description));
            
            if (endTime <= startTime)
                throw new DomainException("End time must be after start time");
            
            if (requiredVolunteers <= 0)
                throw new DomainException("Required volunteers must be greater than zero");

            Id = Guid.NewGuid();
            EventId = eventId;
            Name = name;
            Description = description;
            StartTime = startTime;
            EndTime = endTime;
            RequiredVolunteers = requiredVolunteers;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }
        
        public Guid EventId { get; private set; }
        
        public string Name { get; private set; }
        
        public string Description { get; private set; }
        
        public TimeOnly StartTime { get; private set; }
        
        public TimeOnly EndTime { get; private set; }
        
        public int RequiredVolunteers { get; private set; }
        
        public DateTime CreatedAt { get; private set; }
        
        public DateTime UpdatedAt { get; private set; }
        
        // Navigation properties
        public Event Event { get; private set; } = null!;
        
        public IReadOnlyCollection<VolunteerAssignment> Assignments => _assignments.AsReadOnly();

        /// <summary>
        /// Updates task details
        /// </summary>
        public void UpdateDetails(string name, string description, TimeOnly startTime, TimeOnly endTime, int requiredVolunteers)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Task name is required", nameof(name));
            
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Task description is required", nameof(description));
            
            if (endTime <= startTime)
                throw new DomainException("End time must be after start time");
            
            if (requiredVolunteers <= 0)
                throw new DomainException("Required volunteers must be greater than zero");

            Name = name;
            Description = description;
            StartTime = startTime;
            EndTime = endTime;
            RequiredVolunteers = requiredVolunteers;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Assigns a volunteer to this task
        /// </summary>
        public VolunteerAssignment AssignVolunteer(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
            
            if (_assignments.Any(a => a.UserId == userId && a.Status != VolunteerStatus.Cancelled))
                throw new DomainException("User is already assigned to this task");
            
            if (GetConfirmedVolunteerCount() >= RequiredVolunteers)
                throw new DomainException("Task already has the required number of volunteers");

            var assignment = new VolunteerAssignment(Id, userId);
            _assignments.Add(assignment);
            UpdatedAt = DateTime.UtcNow;
            
            return assignment;
        }

        /// <summary>
        /// Removes a volunteer assignment
        /// </summary>
        public void RemoveAssignment(Guid assignmentId)
        {
            var assignment = _assignments.FirstOrDefault(a => a.Id == assignmentId);
            if (assignment == null)
                throw new DomainException("Assignment not found");
            
            _assignments.Remove(assignment);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the number of confirmed volunteers
        /// </summary>
        public int GetConfirmedVolunteerCount()
        {
            return _assignments.Count(a => a.Status == VolunteerStatus.Confirmed);
        }

        /// <summary>
        /// Checks if the task needs more volunteers
        /// </summary>
        public bool NeedsMoreVolunteers()
        {
            return GetConfirmedVolunteerCount() < RequiredVolunteers;
        }
    }
}