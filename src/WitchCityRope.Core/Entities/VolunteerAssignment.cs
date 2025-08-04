using System;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Represents a volunteer assignment to a specific task
    /// </summary>
    public class VolunteerAssignment
    {
        // Private constructor for EF Core
        private VolunteerAssignment()
        {
        }

        public VolunteerAssignment(Guid taskId, Guid userId)
        {
            if (taskId == Guid.Empty)
                throw new ArgumentException("Task ID cannot be empty", nameof(taskId));
            
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            Id = Guid.NewGuid();
            TaskId = taskId;
            UserId = userId;
            Status = VolunteerStatus.Pending;
            HasTicket = false;
            TicketPrice = 0;
            BackgroundCheckVerified = false;
            AssignedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }
        
        public Guid TaskId { get; private set; }
        
        public Guid UserId { get; private set; }
        
        public VolunteerStatus Status { get; private set; }
        
        public bool HasTicket { get; private set; }
        
        public decimal TicketPrice { get; private set; }
        
        public bool BackgroundCheckVerified { get; private set; }
        
        public DateTime AssignedAt { get; private set; }
        
        public DateTime UpdatedAt { get; private set; }
        
        // Navigation properties
        public VolunteerTask Task { get; private set; } = null!;
        
        // User navigation property removed - user lookups will be handled via UserId

        /// <summary>
        /// Confirms the volunteer assignment
        /// </summary>
        public void Confirm()
        {
            if (Status == VolunteerStatus.Cancelled)
                throw new DomainException("Cannot confirm a cancelled assignment");
            
            Status = VolunteerStatus.Confirmed;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Cancels the volunteer assignment
        /// </summary>
        public void Cancel()
        {
            if (Status == VolunteerStatus.Cancelled)
                throw new DomainException("Assignment is already cancelled");
            
            Status = VolunteerStatus.Cancelled;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the ticket status for this volunteer
        /// </summary>
        public void UpdateTicketStatus(bool hasTicket, decimal ticketPrice)
        {
            if (ticketPrice < 0)
                throw new DomainException("Ticket price cannot be negative");
            
            HasTicket = hasTicket;
            TicketPrice = ticketPrice;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the background check verification status
        /// </summary>
        public void UpdateBackgroundCheckStatus(bool verified)
        {
            BackgroundCheckVerified = verified;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Status of a volunteer assignment
    /// </summary>
    public enum VolunteerStatus
    {
        /// <summary>
        /// Volunteer has been assigned but not yet confirmed
        /// </summary>
        Pending,

        /// <summary>
        /// Volunteer has confirmed their assignment
        /// </summary>
        Confirmed,

        /// <summary>
        /// Volunteer assignment has been cancelled
        /// </summary>
        Cancelled
    }
}